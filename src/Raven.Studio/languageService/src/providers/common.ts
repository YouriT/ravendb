import { CandidatesCollection } from "antlr4-c3/out/src/CodeCompletionCore";
import { Scanner } from "../scanner";
import { RqlParser } from "../RqlParser";
import { ProgContext } from "../generated/BaseRqlParser";

export const AUTOCOMPLETE_SCORING = {
    operator: 1007,
    function: 1004,
    keyword: 1004,
    field: 1020,
    index: 1010,
    collection: 1010
}

export const AUTOCOMPLETE_META = {
    operator: "operator",
    keyword: "keyword",
    function: "function",
    field: "field",
    index: "index",
    collection: "collection"
};

export interface AutocompleteProvider {
    collect?: (scanner: Scanner, candidates: CandidatesCollection, parser: RqlParser, parseTree: ProgContext, writtenText: string) => autoCompleteWordList[]; 
    collectAsync?: (scanner: Scanner, candidates: CandidatesCollection, parser: RqlParser, parseTree: ProgContext, writtenText: string) => Promise<autoCompleteWordList[]>;    
}