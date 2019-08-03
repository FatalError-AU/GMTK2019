using UnityEngine;
using UnityEngine.UI;

namespace FX
{
    public class SpriteAnimation : MonoBehaviour
    {
        public Sprite[] frames;
        public float fps = 15F;
        public int frame;

        private float timer;
        private Image image;

        private void Awake()
        {
            image = GetComponent<Image>();
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer > 1F / fps)
            {
                timer = 0F;
                frame++;
                if (frame >= frames.Length)
                    frame = 0;

                image.sprite = frames[frame];
            }
        }
    }
}
