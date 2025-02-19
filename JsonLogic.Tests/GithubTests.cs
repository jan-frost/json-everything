﻿using System.Text.Json;
using System.Text.Json.Nodes;
using Json.More;
using NUnit.Framework;

namespace Json.Logic.Tests;

public class GithubTests
{
	[Test]
	public void Issue183_RuleEvaluatesWrong_Truthy()
	{
		var logic = JsonLogic.If(
			JsonLogic.Variable("data.sub"),
			JsonLogic.StrictEquals(
				JsonLogic.Variable("data.sub.0.element"),
				"12345"
			),
			true
		);
		var json = JsonNode.Parse(@"{
   ""data"": {
    ""sub"": [
      {
        ""element"": ""12345""
      }
    ]
  }
}");

		var result = logic.Apply(json);

		JsonAssert.AreEquivalent(true, result);
	}
	[Test]
	public void Issue183_RuleEvaluatesWrong_Falsy()
	{
		var logic = JsonLogic.If(
			JsonLogic.Variable("data.sub"),
			JsonLogic.StrictEquals(
				JsonLogic.Variable("data.sub.0.element"),
				"12345"
			),
			true
		);
		var json = JsonNode.Parse(@"{
   ""data"": {
    ""sub"": [
      {
        ""element"": ""12346""
      }
    ]
  }
}");

		var result = logic.Apply(json);

		JsonAssert.AreEquivalent(false, result);
	}

	[Test]
	public void Issue183_RuleEvaluatesWrong2_Falsy()
	{
		var jsonRule = "{\"and\":[{\"if\":[{\"var\":\"data.r.0\"},{\"in\":[{\"var\":\"data.r.0.tg\"},[\"140539006\"]]},true]},{\"if\":[{\"var\":\"data.t.0\"},{\"in\":[{\"var\":\"data.t.0.tg\"},[\"140539006\"]]},true]},{\"if\":[{\"var\":\"data.v.0\"},{\"in\":[{\"var\":\"data.v.0.tg\"},[\"140539006\"]]},true]}]}";
		var logic = JsonNode.Parse(jsonRule);
		var rule = JsonSerializer.Deserialize<Rule>(logic.AsJsonString());


		var data = JsonNode.Parse("{\"data\":{\"r\":[{\"tg\":\"140539006\"}],\"t\":[{\"tg\":\"140539006\"}],\"v\":[{\"tg\":\"Test\"}]}}");

		var result = rule!.Apply(data);

		JsonAssert.AreEquivalent(false, result);
	}

	[Test]
	public void Issue183_RuleEvaluatesWrong3_Falsy()
	{
		var jsonRule = "{\"===\":[{\"reduce\":[[{\"var\":\"data.r\"},{\"var\":\"data.t\"},{\"var\":\"data.v\"}],{\"\\u002B\":[{\"var\":\"accumulator\"},{\"if\":[{\"var\":\"current.0\"},1,0]}]},0]},1]}";
		var logic = JsonNode.Parse(jsonRule);
		var rule = JsonSerializer.Deserialize<Rule>(logic.AsJsonString());


		var data = JsonNode.Parse("{\"data\":{\"r\":[{\"tg\":\"140539006\"},{\"tg\":\"140539006\"}]}}");

		var result = rule!.Apply(data);

		JsonAssert.AreEquivalent(true, result);
	}

	[Test]
	public void Issue263_SomeInTest()
	{
		var rule = JsonLogic.Some(
			JsonLogic.Variable("x"),
			JsonLogic.In(
				JsonLogic.Variable(""),
				JsonLogic.Variable("y")
			)
		);

		var data = JsonNode.Parse("{\"x\":[-1, 0, 1],\"y\":[2, 3, 1]}");

		JsonAssert.IsTrue(rule.Apply(data));
	}
}