﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Json.Pointer;

namespace Json.Schema;

/// <summary>
/// Handles `additionalProperties`.
/// </summary>
[Applicator]
[SchemaPriority(10)]
[SchemaKeyword(Name)]
[SchemaDraft(Draft.Draft6)]
[SchemaDraft(Draft.Draft7)]
[SchemaDraft(Draft.Draft201909)]
[SchemaDraft(Draft.Draft202012)]
[Vocabulary(Vocabularies.Applicator201909Id)]
[Vocabulary(Vocabularies.Applicator202012Id)]
[JsonConverter(typeof(AdditionalPropertiesKeywordJsonConverter))]
public class AdditionalPropertiesKeyword : IJsonSchemaKeyword, IRefResolvable, ISchemaContainer, IEquatable<AdditionalPropertiesKeyword>
{
	internal const string Name = "additionalProperties";

	/// <summary>
	/// The schema by which to validation additional properties.
	/// </summary>
	public JsonSchema Schema { get; }

	static AdditionalPropertiesKeyword()
	{
		ValidationResults.RegisterConsolidationMethod(ConsolidateAnnotations);
	}
	/// <summary>
	/// Creates a new <see cref="AdditionalPropertiesKeyword"/>.
	/// </summary>
	/// <param name="value">The keyword's schema.</param>
	public AdditionalPropertiesKeyword(JsonSchema value)
	{
		Schema = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <summary>
	/// Provides validation for the keyword.
	/// </summary>
	/// <param name="context">Contextual details for the validation process.</param>
	public void Validate(ValidationContext context)
	{
		context.EnterKeyword(Name);
		var schemaValueType = context.LocalInstance.GetSchemaValueType();
		if (schemaValueType != SchemaValueType.Object)
		{
			context.LocalResult.Pass();
			context.WrongValueKind(schemaValueType);
			return;
		}

		context.Options.LogIndentLevel++;
		var overallResult = true;
		var annotation = (context.LocalResult.TryGetAnnotation(PropertiesKeyword.Name) as List<string>)?.ToList();
		List<string> evaluatedProperties;
		var obj = (JsonObject)context.LocalInstance!;
		if (!obj.VerifyJsonObject(context)) return;

		if (context.Options.ValidatingAs is Draft.Draft6 or Draft.Draft7)
		{
			evaluatedProperties = new List<string>();
			var propertiesKeyword = context.LocalSchema.Keywords!.OfType<PropertiesKeyword>().FirstOrDefault();
			if (propertiesKeyword != null)
				evaluatedProperties.AddRange(propertiesKeyword.Properties.Keys);
			var patternPropertiesKeyword = context.LocalSchema.Keywords!.OfType<PatternPropertiesKeyword>().FirstOrDefault();
			if (patternPropertiesKeyword != null)
				evaluatedProperties.AddRange(obj
					.Select(x => x.Key)
					.Where(x => patternPropertiesKeyword.Patterns.All(p => !p.Key.IsMatch(x))));
		}
		else
		{
			if (annotation == null)
			{
				context.Log(() => $"No annotation from {PropertiesKeyword.Name}.");
				evaluatedProperties = new List<string>();
			}
			else
			{
				var annotation1 = annotation;
				context.Log(() => $"Annotation from {PropertiesKeyword.Name}: [{string.Join(",", annotation1.Select(x => $"'{x}'"))}]");
				evaluatedProperties = annotation;
			}
			annotation = (context.LocalResult.TryGetAnnotation(PatternPropertiesKeyword.Name) as List<string>)?.ToList();
			if (annotation == null)
				context.Log(() => $"No annotation from {PatternPropertiesKeyword.Name}.");
			else
			{
				context.Log(() => $"Annotation from {PatternPropertiesKeyword.Name}: [{string.Join(",", annotation.Select(x => $"'{x}'"))}]");
				evaluatedProperties.AddRange(annotation);
			}
		}
		var additionalProperties = obj.Where(p => !evaluatedProperties.Contains(p.Key)).ToList();
		evaluatedProperties.Clear();
		foreach (var property in additionalProperties)
		{
			if (!obj.TryGetPropertyValue(property.Key, out var item))
			{
				context.Log(() => $"Property '{property.Key}' does not exist. Skipping.");
				continue;
			}

			context.Log(() => $"Validating property '{property.Key}'.");
			context.Push(context.InstanceLocation.Combine(PointerSegment.Create($"{property.Key}")), item);
			Schema.ValidateSubschema(context);
			var localResult = context.LocalResult.IsValid;
			overallResult &= localResult;
			context.Log(() => $"Property '{property.Key}' {localResult.GetValidityString()}.");
			context.Pop();
			if (!overallResult && context.ApplyOptimizations) break;
			if (localResult)
				evaluatedProperties.Add(property.Key);
		}
		context.Options.LogIndentLevel--;

		if (context.LocalResult.TryGetAnnotation(Name) is List<string> list)
			list.AddRange(evaluatedProperties);
		else
			context.LocalResult.SetAnnotation(Name, evaluatedProperties);

		if (overallResult)
			context.LocalResult.Pass();
		else
			context.LocalResult.Fail();
		context.ExitKeyword(Name, context.LocalResult.IsValid);
	}

	private static void ConsolidateAnnotations(ValidationResults localResults)
	{
		var allProperties = localResults.NestedResults.Select(c => c.TryGetAnnotation(Name))
			.Where(a => a != null)
			.Cast<List<string>>()
			.SelectMany(a => a)
			.Distinct()
			.ToList();
		if (localResults.TryGetAnnotation(Name) is List<string> annotation)
			annotation.AddRange(allProperties);
		else if (allProperties.Any())
			localResults.SetAnnotation(Name, allProperties);
	}

	void IRefResolvable.RegisterSubschemas(SchemaRegistry registry, Uri currentUri)
	{
		Schema.RegisterSubschemas(registry, currentUri);
	}

	/// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
	/// <param name="other">An object to compare with this object.</param>
	/// <returns>true if the current object is equal to the <paramref name="other">other</paramref> parameter; otherwise, false.</returns>
	public bool Equals(AdditionalPropertiesKeyword? other)
	{
		if (ReferenceEquals(null, other)) return false;
		if (ReferenceEquals(this, other)) return true;
		return Equals(Schema, other.Schema);
	}

	/// <summary>Determines whether the specified object is equal to the current object.</summary>
	/// <param name="obj">The object to compare with the current object.</param>
	/// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
	public override bool Equals(object obj)
	{
		return Equals(obj as AdditionalPropertiesKeyword);
	}

	/// <summary>Serves as the default hash function.</summary>
	/// <returns>A hash code for the current object.</returns>
	public override int GetHashCode()
	{
		return Schema.GetHashCode();
	}
}

internal class AdditionalPropertiesKeywordJsonConverter : JsonConverter<AdditionalPropertiesKeyword>
{
	public override AdditionalPropertiesKeyword Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var schema = JsonSerializer.Deserialize<JsonSchema>(ref reader, options)!;

		return new AdditionalPropertiesKeyword(schema);
	}
	public override void Write(Utf8JsonWriter writer, AdditionalPropertiesKeyword value, JsonSerializerOptions options)
	{
		writer.WritePropertyName(AdditionalPropertiesKeyword.Name);
		JsonSerializer.Serialize(writer, value.Schema, options);
	}
}