﻿using System;
using System.Net.Http;
using Raven.Client.Blittable;
using Raven.Client.Commands;
using Raven.Client.Data;
using Raven.Client.Document;
using Raven.Client.Extensions;
using Raven.Client.Http;
using Raven.Client.Json;
using Sparrow.Json;

namespace Raven.Client.Operations.Databases
{
    public class CreateDatabaseOperation : IAdminOperation<CreateDatabaseResult>
    {
        private readonly DatabaseDocument _databaseDocument;

        public CreateDatabaseOperation(DatabaseDocument databaseDocument)
        {
            MultiDatabase.AssertValidName(databaseDocument.Id);
            _databaseDocument = databaseDocument;
        }

        public RavenCommand<CreateDatabaseResult> GetCommand(DocumentConvention conventions, JsonOperationContext context)
        {
            return new CreateDatabaseCommand(conventions, context, _databaseDocument);
        }

        private class CreateDatabaseCommand : RavenCommand<CreateDatabaseResult>
        {
            private readonly JsonOperationContext _context;
            private readonly BlittableJsonReaderObject _databaseDocument;
            private readonly string _databaseName;

            public CreateDatabaseCommand(DocumentConvention conventions, JsonOperationContext context, DatabaseDocument databaseDocument)
            {
                if (conventions == null)
                    throw new ArgumentNullException(nameof(conventions));
                if (context == null)
                    throw new ArgumentNullException(nameof(context));
                if (databaseDocument == null)
                    throw new ArgumentNullException(nameof(databaseDocument));

                _context = context;
                _databaseName = databaseDocument.Id;
                _databaseDocument = new EntityToBlittable(null).ConvertEntityToBlittable(databaseDocument, conventions, context);
            }

            public override HttpRequestMessage CreateRequest(ServerNode node, out string url)
            {
                url = $"{node.Url}/admin/databases?name={_databaseName}";

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Put,
                    Content = new BlittableJsonContent(stream =>
                    {
                        _context.Write(stream, _databaseDocument);
                    })
                };

                return request;
            }

            public override void SetResponse(BlittableJsonReaderObject response, bool fromCache)
            {
                if (response == null)
                    ThrowInvalidResponse();

                Result = JsonDeserializationClient.CreateDatabaseResult(response);
            }

            public override bool IsReadRequest => false;
        }
    }
}