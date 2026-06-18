using System;
using UnityEngine;

namespace NANINuko
{
    public static class NANINukoDialogHelper
    {
        public static void OpenTeamNameDialog()
        {
            OpenTextInputDialog(
                "チーム名を入力してください",
                NANINukoConfig.TeamName?.Value ?? "",
                allowEmpty: false,
                onCommitted: newName =>
                {
                    NANINukoConfig.TeamName.Value = newName;
                    NANINukoConfig.Save();
                    Debug.Log("[NANINuko] TeamName set to: " + newName);
                }
            );
        }

        public static void OpenHonorificDialog()
        {
            OpenTextInputDialog(
                "敬称を入力してください",
                NANINukoConfig.Honorific?.Value ?? "さん",
                allowEmpty: true,
                onCommitted: newValue =>
                {
                    NANINukoConfig.Honorific.Value = newValue;
                    NANINukoConfig.Save();
                    Debug.Log("[NANINuko] Honorific set to: " + newValue);
                }
            );
        }

        private static void OpenTextInputDialog(
            string prompt,
            string current,
            bool allowEmpty,
            Action<string> onCommitted
        )
        {
            try
            {
                Dialog.InputName(
                    prompt,
                    current,
                    (cancelled, newName) =>
                    {
                        if (cancelled)
                            return;

                        newName = (newName ?? "").Trim();

                        if (!allowEmpty && string.IsNullOrEmpty(newName))
                            return;

                        onCommitted?.Invoke(newName);
                    },

                    Dialog.InputType.Default
                );
            }
            catch (Exception ex)
            {
                Debug.LogError("[NANINuko] OpenTextInputDialog failed: " + ex);
            }
        }
    }
}
