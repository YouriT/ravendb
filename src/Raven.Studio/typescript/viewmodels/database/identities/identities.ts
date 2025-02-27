import viewModelBase = require("viewmodels/viewModelBase");
import eventsCollector = require("common/eventsCollector");
import virtualGridController = require("widgets/virtualGrid/virtualGridController");
import textColumn = require("widgets/virtualGrid/columns/textColumn");
import actionColumn = require("widgets/virtualGrid/columns/actionColumn");
import virtualColumn = require("widgets/virtualGrid/columns/virtualColumn");
import columnPreviewPlugin = require("widgets/virtualGrid/columnPreviewPlugin");
import getIdentitiesCommand = require("commands/database/identities/getIdentitiesCommand");
import seedIdentityCommand = require("commands/database/identities/seedIdentityCommand");
import genUtils = require("common/generalUtils");

class identity {
    prefix = ko.observable<string>();
    prefixWithPipe: KnockoutComputed<string>;
    prefixWithoutPipe: KnockoutComputed<string>;
    prefixAlreadyExists = ko.observable<boolean>(false);
    
    value = ko.observable<number>();
    currentValue = ko.observable<number>();
    warnAboutSmallerValue: KnockoutComputed<boolean>;
    
    nextDocumentText: KnockoutComputed<string>;
    
    validationGroup = ko.validatedObservable();
    
    constructor(prefix: string, value: number) {
        this.prefix(prefix);
        this.value(value);
        this.currentValue(value);
        
        this.initObservables();
        this.initValidation();
    }
    
    private initObservables(): void {
        this.prefixWithPipe = ko.pureComputed(() => {
            let prefix = this.prefix();
            
            if (prefix && !prefix.endsWith("|")) {
                prefix += "|";
            }
            
            return prefix;
        });
        
        this.prefixWithoutPipe = ko.pureComputed(() => {
            let prefix = this.prefix();
            
            if (prefix.endsWith("|")) {
                return prefix.slice(0, -1);
            }

            return prefix;
        });
        
        this.nextDocumentText = ko.pureComputed(() => {
           return `<span>The next document that will be created with Prefix: "<strong>${genUtils.escapeHtml(this.prefixWithPipe())}</strong>"
                   will have ID: "<strong>${genUtils.escapeHtml(this.prefixWithoutPipe())}/${this.value() + 1}</strong>"</span>`;
        });
        
        this.warnAboutSmallerValue = ko.pureComputed(() => {
           return this.value() < this.currentValue();
        });
    }

    private initValidation(): void {
        this.prefix.extend({
            required: true,
            validation: [
                {
                    validator: () => !this.prefixAlreadyExists(),
                    message: "Prefix already exists"
                }
            ]
        });

        this.value.extend({
            required: true,
            digit: true
        });
        
        this.validationGroup = ko.validatedObservable({
            name: this.prefix,
            value: this.value
        })
    }
    
    static empty() {
        return new identity("", null);
    }
}

class identities extends viewModelBase {

    editedIdentityItem = ko.observable<identity>(null);
    isNewIdentity = ko.observable<boolean>(false);
    
    filter = ko.observable<string>();

    identityPrefixList: string[] = [];
    
    private gridController = ko.observable<virtualGridController<identity>>();
    private columnPreview = new columnPreviewPlugin<identity>();

    clientVersion = viewModelBase.clientVersion;
    
    constructor() {
        super();
        this.bindToCurrentInstance("saveIdentity", "addNewIdentity", "cancel");
        this.initObservables();
    }

    private initObservables(): void {
        this.filter.throttle(500).subscribe(() => this.filterIdentities());
    }
    
    private filterIdentities(): void {
        this.gridController().reset();
    }

    private fetchIdentities(): JQueryPromise<pagedResult<identity>> {
        const task = $.Deferred<pagedResult<identity>>();

        new getIdentitiesCommand(this.activeDatabase())
            .execute()
            .done((identities: dictionary<number>) => {
                
                const mappedIdentities = _.map(identities, (value, key): identity => {
                    return new identity(key, value);
                });

                this.identityPrefixList = mappedIdentities.map(x => x.prefixWithoutPipe());

                let filteredIdentities = this.filter() ? 
                    mappedIdentities.filter(x => x.prefix().toLocaleLowerCase().includes(this.filter().toLocaleLowerCase())) : 
                    mappedIdentities;
                
                filteredIdentities = _.sortBy(filteredIdentities, x => x.prefix());

                task.resolve({
                    totalResultCount: filteredIdentities.length,
                    items: filteredIdentities
                });
            });

        return task;
    }
    
    compositionComplete() {
        super.compositionComplete();

        const grid = this.gridController();
        grid.headerVisible(true);
       
        const prefixColumn = new textColumn<identity>(grid, x => x.prefix(), "Document ID Prefix", "60%", { sortable: x => x.prefix() });
        const valueColumn = new textColumn<identity>(grid, x => x.value().toLocaleString(), "Value", "30%", { sortable: x => x.value() });
        const editColumn = new actionColumn<identity>(grid,
            x => this.editIdentity(x),
            "Edit",
            `<i class="icon-edit"></i>`,
            "10%",
            { title: () => 'Edit identity value', hide: () => this.isReadOnlyAccess() });

        const gridColumns = this.isReadOnlyAccess() ? [prefixColumn, valueColumn] : [prefixColumn, valueColumn, editColumn];
        
        grid.init(() => this.fetchIdentities(), () => gridColumns);
        
        this.columnPreview.install(".js-identities-grid", ".js-identities-tooltip",
            (identityItem: identity, column: virtualColumn, e: JQueryEventObject, onValue: (context: any, valueToCopy?: string) => void) => {
            if (column instanceof textColumn) {
                onValue(column.getCellValue(identityItem));
            }
        });
    }

    addNewIdentity($event: JQueryEventObject): void {
        eventsCollector.default.reportEvent("identity", "new");
        this.isNewIdentity(true);
        this.editedIdentityItem(identity.empty())

        this.editedIdentityItem().prefix.subscribe(() => {
            const item = this.editedIdentityItem();
            item.prefixAlreadyExists(this.identityPrefixList.find(x => x.toLocaleLowerCase() === item.prefixWithoutPipe().toLocaleLowerCase()) !== undefined);
        });
    }
    
    editIdentity(identityItem: identity): void {
        this.isNewIdentity(false);
        this.editedIdentityItem(identityItem);
    }
    
    cancel(): void {
        this.isNewIdentity(false);
        this.editedIdentityItem(null);
    }

    saveIdentity(): void {
        const item = this.editedIdentityItem();
        const prefix = item.prefixWithPipe();
        
        if (!this.isValid(item.validationGroup)) {
            return;
        }

        new seedIdentityCommand(this.activeDatabase(), prefix, item.value())
            .execute()
            .done(() => {
                this.editedIdentityItem(null);
                this.gridController().reset();
            });
    }
}

export = identities;
