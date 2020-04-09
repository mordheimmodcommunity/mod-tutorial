using Mono.Data.Sqlite;
using System.Collections.Generic;
using UnityEngine;

public class UnitJoinCombatStyleData : DataCore
{
	public UnitId UnitId
	{
		get;
		private set;
	}

	public CombatStyleId CombatStyleId
	{
		get;
		private set;
	}

	public int Ratio
	{
		get;
		private set;
	}

	public override void Populate(SqliteDataReader reader)
	{
		UnitId = (UnitId)reader.GetInt32(0);
		CombatStyleId = (CombatStyleId)reader.GetInt32(1);
		Ratio = reader.GetInt32(2);
	}

	public static UnitJoinCombatStyleData GetRandomRatio(List<UnitJoinCombatStyleData> datas, Tyche tyche, Dictionary<UnitId, int> modifiers = null)
	{
		int num = 0;
		List<int> list = new List<int>();
		for (int i = 0; i < datas.Count; i++)
		{
			int num2 = datas[i].Ratio;
			if (modifiers != null && modifiers.ContainsKey(datas[i].UnitId))
			{
				num2 = Mathf.Clamp(num2 + modifiers[datas[i].UnitId], 0, int.MaxValue);
			}
			num += num2;
			list.Add(num);
		}
		int num3 = tyche.Rand(0, num);
		for (int j = 0; j < list.Count; j++)
		{
			if (num3 < list[j])
			{
				return datas[j];
			}
		}
		return null;
	}
}
