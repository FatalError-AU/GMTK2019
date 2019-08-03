using UnityEngine;

namespace Enemy
{
    public class EnemyHealth : MonoBehaviour
    {
        public float health = 5F;
        
        public void Damage(float damage)
        {
            health -= damage;
            
            if(health <= 0F)
                Destroy(gameObject);
        }
    }
}