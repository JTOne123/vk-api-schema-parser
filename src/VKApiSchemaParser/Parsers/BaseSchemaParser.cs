﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using VKApiSchemaParser.Extensions;
using VKApiSchemaParser.Models;

namespace VKApiSchemaParser.Parsers
{
    internal abstract class BaseSchemaParser<T>
    {
        protected enum ObjectParsingOptions
        {
            Unnamed,
            Named,
            NamedAndRegistered
        }

        protected abstract string SchemaUrl { get; }

        public async Task<T> ParseAsync()
        {
            var schema = await InitializeAsync(SchemaUrl).ConfigureAwait(false);
            return ParseSchema(schema);
        } 

        protected async Task<JSchema> InitializeAsync(string schemaUrl)
        {
            var rawSchema = await SchemaLoader.Instance.LoadAsync(schemaUrl).ConfigureAwait(false);
            return JSchema.Parse(rawSchema);
        }

        protected abstract T ParseSchema(JSchema schema);

        protected abstract ApiObject ResolveReference(string referencePath);

        protected abstract ApiObject ParseObject(JToken token, ObjectParsingOptions options);

        protected ApiObject ParseNestedObject(JToken token)
        {
            var referencePath = token.GetPropertyAsString(JsonStringConstants.Reference);

            if (!string.IsNullOrWhiteSpace(referencePath))
            {
                return ResolveReference(referencePath);
            }

            return ParseObject(token, ObjectParsingOptions.Unnamed);
        }

        protected void FillType(ApiObject obj, JToken token)
        {
            var type = token.GetPropertyAsArray(JsonStringConstants.Type)?.Count() > 1 ?
                JsonStringConstants.Multiple :
                token.GetPropertyAsString(JsonStringConstants.Type);

            obj.Type = ObjectTypeMapper.Map(type);
        }

        protected void FillProperties(ApiObject obj, JToken token)
        {
            var requiredProperties = token.GetPropertyAsArray(JsonStringConstants.Required)?.ToArray();

            obj.Properties = GetProperties(token, JsonStringConstants.Properties, requiredProperties);
            obj.PatternProperties = GetProperties(token, JsonStringConstants.PatternProperties, requiredProperties);

            obj.MinProperties = token.GetPropertyAsInteger(JsonStringConstants.MinProperties);
            obj.MaxProperties = token.GetPropertyAsInteger(JsonStringConstants.MaxProperties);
            obj.AdditionalProperties = token.GetPropertyAsBoolean(JsonStringConstants.AdditionalProperties) == true;
        }

        protected void FillReference(ApiObject obj, JToken token)
        {
            var reference = token.GetPropertyAsString(JsonStringConstants.Reference);

            if (!string.IsNullOrWhiteSpace(reference))
            {
                obj.Reference = ResolveReference(reference);
            }
        }

        protected void FillOther(ApiObject obj, JToken token)
        {
            obj.Description = token.GetPropertyAsString(JsonStringConstants.Description);
            obj.Minimum = token.GetPropertyAsInteger(JsonStringConstants.Minimum);
            obj.Enum = token.GetPropertyAsArray(JsonStringConstants.Enum);
            obj.EnumNames = token.GetPropertyAsArray(JsonStringConstants.EnumNames);
            obj.Items = token.SelectPropertyOrDefault(JsonStringConstants.Items, ParseNestedObject);
            obj.AllOf = token.SelectPropertyOrDefault(JsonStringConstants.AllOf, t => t.Select(ParseNestedObject));
            obj.OneOf = token.SelectPropertyOrDefault(JsonStringConstants.OneOf, t => t.Select(ParseNestedObject));
        }

        private IEnumerable<ApiObject> GetProperties(JToken token, string propertyName, IEnumerable<string> requiredProperties)
        {
            var parsedProperties = token.SelectPropertyOrDefault(propertyName, t => t
                .Where(p => p.First != null)
                .Select(p =>
                {
                    var newObject = ParseObject(p.First, ObjectParsingOptions.Named);

                    if (requiredProperties != null)
                    {
                        newObject.IsRequired = requiredProperties.Contains(newObject.Name);
                    }

                    return newObject;
                }))
                ?.ToArray();

            var duplicateProperties = parsedProperties
                ?.GroupBy(p => p.Name)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToArray();

            if (duplicateProperties != null && duplicateProperties.Any())
            {
                var propertiesString = string.Join(", ", duplicateProperties);
                throw new Exception($"Duplicate properties {propertiesString}");
            }

            return parsedProperties;
        }
    }
}
