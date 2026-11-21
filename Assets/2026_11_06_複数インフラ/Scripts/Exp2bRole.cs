using UnityEngine;

// このビルドが「正面（Main）」か「左右（Side）」かを判定する。
// 左右ビルドの手順書「2. CameraMasterBicycle の BicycleSideUpdater にチェックを入れる」を目印にする：
//   BicycleSideUpdater が有効 → 左右のビルド、無効 → 正面のビルド（エディタでの確認も正面扱い）
public static class Exp2bRole
{
    public static bool IsSideBuild()
    {
        foreach (var u in Object.FindObjectsOfType<BicycleSideUpdater>())
            if (u.enabled && u.gameObject.activeInHierarchy) return true;
        return false;
    }
}
