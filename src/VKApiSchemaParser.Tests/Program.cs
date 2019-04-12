﻿using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using VKApiSchemaParser.Models.Schemas;

namespace VKApiSchemaParser.Tests
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var schema = await VKApiSchema.ParseAsync();

            Console.WriteLine("Schema processed");

            Console.WriteLine("Processing objects...");
            await CheckObjects(schema);

            Console.WriteLine("Processing responses...");
            await CheckResonses(schema);

            Console.WriteLine("Processing methods...");
            await CheckMethods(schema);

            Console.WriteLine("All complete!");
            Console.ReadKey();
        }

        public static async Task CheckObjects(ApiSchema schema)
        {
            var objects = schema.Objects;
            var serializedSchema = SerializeObject(objects);
            await SaveToFileAsync(serializedSchema, "objects");
        }

        public static async Task CheckResonses(ApiSchema schema)
        {
            var responses = schema.Responses;
            var serializedSchema = SerializeObject(responses);
            await SaveToFileAsync(serializedSchema, "responses");
        }

        public static async Task CheckMethods(ApiSchema schema)
        {
            var methods = schema.Methods;
            var serializedSchema = SerializeObject(methods);
            await SaveToFileAsync(serializedSchema, "methods");
        }

        private static string SerializeObject(object schema)
        {
            return JsonConvert.SerializeObject(schema, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            });
        }

        private static async Task SaveToFileAsync(string data, string prefix)
        {
            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var filePath = Path.Combine(desktopPath, $"{prefix}-{DateTime.Now:HHmmss}.json");

            await File.WriteAllTextAsync(filePath, data);
        }
    }
}
