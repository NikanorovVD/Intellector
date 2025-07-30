#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ApplyPlatformDefines
{
    public static string[] Paths = { "Assets/Plugins/Packages/SignalR" };
    public const string Extension = "t:Object";
    public const string NetStandard = "NET_STANDARD";
    public const string NotNetStandard = "!NET_STANDARD";

    [MenuItem(itemName: "Tools/Apply SignalR Constraints")]
    public static void ApplyDefines()

    {
        //Говорим Unity, что мы начинаем редактирование ассетов и пока мы не закончим, 
        //не нужно импортировать их и тратить очень много времени на это
        AssetDatabase.StartAssetEditing();
        //При любом исходе выполнения нашего скрипта мы должны вызвать
        //AssetDatabase.StopAssetEditing(); после окончания изменения ассетов
        try
        {
            Execute();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        AssetDatabase.StopAssetEditing();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void Execute()
    {
        var assets = AssetDatabase.FindAssets(Extension, Paths);
        var constraints = new List<string>();
        foreach (var guid in assets)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            //Получаем по пути к ассету его импортер для управления его настройками
            var importer = AssetImporter.GetAtPath(path);
            //Нас интересуют именно импортеры dll, они в unity идут как PluginImporter
            //поэтому берем только их
            if (importer is not PluginImporter pluginImporter) continue;

            var restriction = path.Contains("netstandard") ? NetStandard : NotNetStandard;

            //формируем новый список ограничения для dll
            constraints.Clear();
            constraints.AddRange(pluginImporter.DefineConstraints);
            constraints.RemoveAll(
                x => x.Equals(NetStandard, StringComparison.OrdinalIgnoreCase) ||
                     x.Equals(NotNetStandard, StringComparison.OrdinalIgnoreCase));

            constraints.Add(restriction);
            //Задаем новые ограничения по тому, в какой папке находится dll
            pluginImporter.DefineConstraints = constraints.ToArray();

            Debug.Log($"ADD constraints {restriction} to {path}");

            //Сохраняем настройки
            pluginImporter.SaveAndReimport();
        }
    }
}
#endif