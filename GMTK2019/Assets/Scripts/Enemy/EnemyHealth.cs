using UnityEngine;

namespace Enemy
{
    public class EnemyHealth : MonoBehaviour
    {
        public float health = 5F;

        public void Damage(float damage)
        {
            health -= damage;

            if (health <= 0F)
            {
                bool cancel = false;
                
                foreach (IDeath death in GetComponents<IDeath>())
                    death.OnDying(ref cancel);
                
                if(cancel)
                    Destroy(this);
                else
                    Destroy(gameObject);
            }
        }

        public interface IDeath
        {
            void OnDying(ref bool cancel);
        }
    }
}