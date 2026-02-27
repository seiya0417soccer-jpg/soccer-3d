using UnityEngine;

public class MinoController : MonoBehaviour
{
    float fallTime = 0;
    public float fallInterval = 1.0f;
    bool isLocked = false; // 【これ！】2個出しを絶対に防ぐ門番フラグ

    void Update()
    {
        // すでに着地（isLockedがtrue）してたら、これ以降のUpdateは一切動かさない
        if (isLocked) return;

        // 1. 自動落下
        fallTime += Time.deltaTime;
        if (fallTime >= fallInterval)
        {
            if (CanMove(new Vector3(0, -1, 0)))
            {
                transform.position += new Vector3(0, -1, 0);
            }
            else
            {
                LockAndBoost(); // ここで着地！
            }
            fallTime = 0;
        }

        // 2. 左右移動
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (CanMove(new Vector3(-1, 0, 0))) transform.position += new Vector3(-1, 0, 0);
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (CanMove(new Vector3(1, 0, 0))) transform.position += new Vector3(1, 0, 0);
        }

        // 3. 回転
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            transform.Rotate(0, 0, 90);
            if (!CanMove(Vector3.zero)) transform.Rotate(0, 0, -90);
        }
    }

    void LockAndBoost()
    {
        // 【重要】もし門番がすでに閉まってたら、何もしないで帰る
        if (isLocked) return;
        isLocked = true; // ここで門を閉める（二度と呼ばれない）

        // 1. 地図（grid）に記録して、次のミノが「積み上がる」ようにする
        foreach (Transform child in transform)
        {
            int x = Mathf.RoundToInt(child.position.x - 100);
            int y = Mathf.RoundToInt(child.position.y);
            if (x >= 0 && x < 12 && y >= 0 && y < 25)
            {
                TetrisManager.grid[x, y] = child;
            }
        }

        // 2. 勇者加速
        GameObject[] yushas = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject yusha in yushas)
        {
            yusha.SendMessage("Boost", SendMessageOptions.DontRequireReceiver);
        }

        // 3. 【ここがポイント】次のミノを1個だけ召喚！
        if (TetrisManager.instance != null)
        {
            TetrisManager.instance.SpawnMino();
        }

        this.enabled = false; // 自分のスクリプトを自害させて止める
    }

    bool CanMove(Vector3 direction)
    {
        foreach (Transform child in transform)
        {
            // 未来の座標を計算
            Vector3 nextPos = child.position + direction;

            // 【修正ポイント】100を「100f」にして、四捨五入を確実にする
            int x = Mathf.RoundToInt(nextPos.x - 100f);
            int y = Mathf.RoundToInt(nextPos.y);

            // 1. 枠の外（壁・底）の判定
            if (x <= 0 || x >= 11 || y <= 0) return false;

            // 2. 地図（grid）の判定：そこに誰かいたら進めない
            if (x >= 0 && x < 12 && y >= 0 && y < 25)
            {
                if (TetrisManager.grid[x, y] != null) return false;
            }
        }
        return true;
    }
}