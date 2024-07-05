using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// JsonUtility extension.
/// support primitive type, List and Array
///
/// https://gist.github.com/fuqunaga/b50b49cc08010ba37b07ac01c401a8f0
/// </summary>
internal static class JsonUtilityEx
{
	//Usage:
	//YouObject[] objects = JsonHelper.getJsonArray<YouObject> (jsonString);
	public static T[] getJsonArray<T>(string json)
	{
		string newJson = "{ \"array\": " + json + "}";
		Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
		return wrapper.array;
	}
	//Usage:
	//string jsonString = JsonHelper.arrayToJson<YouObject>(objects);
	public static string arrayToJson<T>(T[] array)
	{
		Wrapper<T> wrapper = new Wrapper<T>();
		wrapper.array = array;
		return JsonUtility.ToJson(wrapper);
	}

	[Serializable]
	private class Wrapper<T>
	{
		public T[] array;
	}
}
