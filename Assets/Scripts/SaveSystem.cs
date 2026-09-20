using UnityEngine;
using System.IO;

public static class SaveSystem
{
    // Propiedad en lugar de campo estático: se evalúa en cada llamada,
    // garantizando que Application.persistentDataPath ya está disponible
    private static string Path => Application.persistentDataPath + "/jugador.json";

    public static void Guardar(PlayerData datos)
    {
        try
        {
            string json = JsonUtility.ToJson(datos, prettyPrint: false);
            File.WriteAllText(Path, json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveSystem] Error al guardar: {e.Message}");
        }
    }

    public static PlayerData Cargar()
    {
        try
        {
            if (File.Exists(Path))
            {
                string json = File.ReadAllText(Path);
                var datos = JsonUtility.FromJson<PlayerData>(json);
                if (datos != null) return datos;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveSystem] Error al cargar: {e.Message}");
        }
        return new PlayerData();
    }

    /// <summary>Borra el guardado (solo para debug).</summary>
    public static void Borrar()
    {
        try { if (File.Exists(Path)) File.Delete(Path); }
        catch { }
    }
}