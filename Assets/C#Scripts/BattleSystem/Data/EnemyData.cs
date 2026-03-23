using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemy", menuName = "Battle/EnemyData")]
public class EnemyData : ScriptableObject
{
    public string enemyName;
    public int maxHp;
    public int baseAttack;
    public int maxTimer;
    public Sprite artwork;
}
