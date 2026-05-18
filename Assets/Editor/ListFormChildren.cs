using UnityEngine;
using UnityEditor;

public static class ListFormChildren
{
    public static string Execute()
    {
        var form = GameObject.Find("Canvas/ReturnPanel/Form");
        if (form == null)
        {
            // Try searching inactive panels
            var canvas = GameObject.Find("Canvas");
            if (canvas != null)
            {
                var t = canvas.transform.Find("ReturnPanel/Form");
                if (t != null) form = t.gameObject;
            }
        }
        if (form == null) return "Form not found";

        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < form.transform.childCount; i++)
        {
            var c = form.transform.GetChild(i);
            sb.AppendLine(i + ": " + c.name + "  (active=" + c.gameObject.activeSelf + ")");
        }
        return sb.ToString();
    }
}
