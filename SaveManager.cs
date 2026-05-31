using UnityEngine;
using System.IO;

public static class SaveManager
{
    private static string saveYolu = Application.persistentDataPath + "/katana_zero_save.json";

    public static void Kaydet(SaveData data)
    {
        string json = JsonUtility.ToJson(data);
        File.WriteAllText(saveYolu, json);
        Debug.Log("Oyun Otomatik Kaydedildi: " + saveYolu);
    }

    public static SaveData Yukle()
    {
        if (File.Exists(saveYolu))
        {
            string json = File.ReadAllText(saveYolu);
            return JsonUtility.FromJson<SaveData>(json);
        }
        return null;
    }

    public static bool KayitVarMi()
    {
        return File.Exists(saveYolu);
    }

    public static void KaydiSil()
    {
        if (File.Exists(saveYolu))
        {
            File.Delete(saveYolu);
            Debug.Log("Mevcut Kayıt Dosyası Silindi.");
        }
    }
}