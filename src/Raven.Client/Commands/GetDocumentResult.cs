﻿using Sparrow.Json;

namespace Raven.Client.Commands
{
    public class GetDocumentResult
    {
        public BlittableJsonReaderArray Includes { get; set; }

        public BlittableJsonReaderArray Results { get; set; }

        public int NextPageStart { get; set; }
    }
}