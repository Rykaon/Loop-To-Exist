using UnityEngine;

public class AnimTuto : MonoBehaviour
{
    // Référence à l'Animator
    private Animator animator;

    // Le nom de l'animation que vous souhaitez jouer
    public string animationName = "NomDeVotreAnimation";

    // Frame à laquelle vous souhaitez démarrer l'animation
    public int startFrame = 0;

    private void Start()
    {
        // Récupérez la référence de l'Animator sur le même GameObject que ce script
        animator = GetComponent<Animator>();

        // Vérifiez si l'Animator existe
        if (animator == null)
        {
            Debug.LogError("Animator non trouvé sur l'objet.");
        }
    }

    private void JouerAnimation()
    {
        // Jouez l'animation
        animator.Play(animationName, 0, (float)startFrame / animator.GetCurrentAnimatorStateInfo(0).length);
    }

    // Vous pouvez appeler cette méthode depuis d'autres parties de votre script selon vos besoins
    private void ExampleUsage()
    {
        JouerAnimation();
    }
}