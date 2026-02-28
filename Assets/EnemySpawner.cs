using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab; // ¢Š«‚·‚é“G
    private GameObject currentEnemy; // Œ»İê‚É‚¢‚é“G

    void Update()
    {
        // “G‚ª‚¢‚È‚¯‚ê‚ÎV‚µ‚­—N‚©‚¹‚é
        if (currentEnemy == null)
        {
            Spawn();
        }
    }

    void Spawn()
    {
        // °(30x30)‚Ì”ÍˆÍ“à‚Éƒ‰ƒ“ƒ_ƒ€‚ÉÀ•W‚ğŒˆ‚ß‚é
        float x = Random.Range(-14f, 14f);
        float z = Random.Range(-14f, 14f);
        Vector3 pos = new Vector3(x, 0.5f, z);

        // “G‚ğ¢Š«‚µ‚ÄcurrentEnemy‚É•Û‘¶
        currentEnemy = Instantiate(enemyPrefab, pos, Quaternion.identity);
    }
}