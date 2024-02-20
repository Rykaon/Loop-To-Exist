using UnityEngine;

public class AnimTuto : MonoBehaviour
{
    // R�f�rence � l'Animator
    private Animator animator;

    // Le nom de l'animation que vous souhaitez jouer
    public string animationName = "NomDeVotreAnimation";

    // Frame � laquelle vous souhaitez d�marrer l'animation
    public int startFrame = 0;

    private void Start()
    {
        // R�cup�rez la r�f�rence de l'Animator sur le m�me GameObject que ce script
        animator = GetComponent<Animator>();

        // V�rifiez si l'Animator existe
        if (animator == null)
        {
            Debug.LogError("Animator non trouv� sur l'objet.");
        }
    }

    private void JouerAnimation()
    {
        // Jouez l'animation
        animator.Play(animationName, 0, (float)startFrame / animator.GetCurrentAnimatorStateInfo(0).length);
    }

    // Vous pouvez appeler cette m�thode depuis d'autres parties de votre script selon vos besoins
    private void ExampleUsage()
    {
        JouerAnimation();
    }
}