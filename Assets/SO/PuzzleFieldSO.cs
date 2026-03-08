using UnityEngine;

[CreateAssetMenu(menuName = "Puzzle/Field Date")]
public class PuzzleFieldSO : ScriptableObject
{
    // --- フィールドサイズ ---
    [SerializeField] private int hight = 22; // 高さ
    [SerializeField] private int wide = 13;  // 横

    public int Wide { get => wide; }
    public int Hight { get => hight; }
}