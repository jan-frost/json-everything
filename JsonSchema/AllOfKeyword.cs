﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Json.Pointer;

namespace Json.Schema;

/// <summary>
/// Handles `allOf`.
/// </summary>
[Applicator]
[SchemaPriority(20)]
[SchemaKeyword(Name)]
[SchemaDraft(Draft.Draft6)]
[SchemaDraft(Draft.Draft7)]
[SchemaDraft(Draft.Draft201909)]
[SchemaDraft(Draft.Draft202012)]
[Vocabulary(Vocabularies.Applicator201909Id)]
[Vocabulary(Vocabularies.Applicator202012Id)]
[JsonConverter(typeof(AllOfKeywordJsonConverter))]
public class AllOfKeyword : IJsonSchemaKeyword, IRefResolvable, ISchemaCollector, IEquatable<AllOfKeyword>
{
	internal const string Name = "allOf";

	/// <summary>
	/// The keywords schema collection.
	/// </summary>
	public IReadOnlyList<JsonSchema> Schemas { get; }

	/// <summary>
	/// Creates a new <see cref="AllOfKeyword"/>.
	/// </summary>
	/// <param name="values">The set of schemas.</param>
	public AllOfKeyword(params JsonSchema[] values)
	{
		Schemas = values.ToList() ?? throw new ArgumentNullException(nameof(values));
	}

	/// <summary>
	/// Creates a new <see cref="AllOfKeyword"/>.
	/// </summary>
	/// <param name="values">The set of schemas.</param>
	public AllOfKeyword(IEnumerable<JsonSchema> values)
	{
		Schemas = values.ToList() ?? throw new ArgumentNullException(nameof(values));
	}

	/// <summary>
	/// Provides validation for the keyword.
	/// </summary>
	/// <param name="context">Contextual details for the validation process.</param>
	public void Validate(ValidationContext context)
	{
		context.EnterKeyword(Name);
		var overallResult = true;
		for (var i = 0; i < Schemas.Count; i++)
		{
			var i1 = i;
			context.Log(() => $"Processing {Name}[{i1}]...");
			var schema = Schemas[i];
			context.Push(subschemaLocation: context.SchemaLocation.Combine(PointerSegment.Create($"{i}")));
			schema.ValidateSubschema(context);
			overallResult &= context.LocalResult.IsValid;
			context.Log(() => $"{Name}[{i1}] {context.LocalResult.IsValid.GetValidityString()}.");
			context.Pop();
			if (!overallResult && context.ApplyOptimizations) break;
		}

		context.LocalResult.ConsolidateAnnotations();
		if (overallResult)
			context.LocalResult.Pass();
		else
			context.LocalResult.Fail();
		context.ExitKeyword(Name, context.LocalResult.IsValid);
	}

	void IRefResolvable.RegisterSubschemas(SchemaRegistry registry, Uri currentUri)
	{
		foreach (var schema in Schemas)
		{
			schema.RegisterSubschemas(registry, currentUri);
		}
	}

	/// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
	/// <param name="other">An object to compare with this object.</param>
	/// <returns>true if the current object is equal to the <paramref name="other">other</paramref> parameter; otherwise, false.</returns>
	public bool Equals(AllOfKeyword? other)
	{
		if (ReferenceEquals(null, other)) return false;
		if (ReferenceEquals(this, other)) return true;
		return Schemas.ContentsEqual(other.Schemas);
	}

	/// <summary>Determines whether the specified object is equal to the current object.</summary>
	/// <param name="obj">The object to compare with the current object.</param>
	/// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
	public override bool Equals(object obj)
	{
		return Equals(obj as AllOfKeyword);
	}

	/// <summary>Serves as the default hash function.</summary>
	/// <returns>A hash code for the current object.</returns>
	public override int GetHashCode()
	{
		return Schemas.GetUnorderedCollectionHashCode();
	}
}

internal class AllOfKeywordJsonConverter : JsonConverter<AllOfKeyword>
{
	public override AllOfKeyword Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.StartArray)
		{
			var schemas = JsonSerializer.Deserialize<List<JsonSchema>>(ref reader, options)!;
			return new AllOfKeyword(schemas);
		}

		var schema = JsonSerializer.Deserialize<JsonSchema>(ref reader, options)!;
		return new AllOfKeyword(schema);
	}
	public override void Write(Utf8JsonWriter writer, AllOfKeyword value, JsonSerializerOptions options)
	{
		writer.WritePropertyName(AllOfKeyword.Name);
		writer.WriteStartArray();
		foreach (var schema in value.Schemas)
		{
			JsonSerializer.Serialize(writer, schema, options);
		}
		writer.WriteEndArray();
	}
}