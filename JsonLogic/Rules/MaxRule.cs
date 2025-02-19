﻿using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

// ReSharper disable PossibleMultipleEnumeration

namespace Json.Logic.Rules;

[Operator("max")]
internal class MaxRule : Rule
{
	private readonly List<Rule> _items;

	public MaxRule(Rule a, params Rule[] more)
	{
		_items = new List<Rule> { a };
		_items.AddRange(more);
	}

	public override JsonNode? Apply(JsonNode? data, JsonNode? contextData = null)
	{
		var items = _items.Select(i => i.Apply(data, contextData))
			.Select(e => new { Type = e.JsonType(), Value = e.Numberify() })
			.ToList();
		var nulls = items.Where(i => i.Value == null);
		if (nulls.Any())
			throw new JsonLogicException($"Cannot find max with {nulls.First().Type}.");
		
		return items.Max(i => i.Value!.Value);
	}
}