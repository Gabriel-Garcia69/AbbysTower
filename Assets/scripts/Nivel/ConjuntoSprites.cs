using UnityEngine;

/// <summary>
/// Un juego de sprites para el cuerpo del jugador (los mismos arreglos de AbbySpriteAnimator, en 8 direcciones:
/// 0=E 1=NE 2=N 3=NO 4=O 5=SO 6=S 7=SE). Un arreglo vacío significa "usa el de Abby". AbbySpriteAnimator.Aplicar lo carga;
/// así Dante puede tener sus sprites solo en Expedicion sin tocar el prefab ni las demás escenas.
/// ExpedicionPiso20 arma el de Dante humano con Resources/DanteHumano (no con Personajes/Dante: esa es su sombra de jefe final).
/// </summary>
[CreateAssetMenu(menuName = "Abby/Conjunto de sprites", fileName = "Dante")]
public class ConjuntoSprites : ScriptableObject
{
    public string nombre = "Dante";
    [Tooltip("Retrato del HUD (cabeza y torso; se usa la mitad de arriba del sprite). Vacío = el de Abby.")]
    public Sprite retrato;
    [Tooltip("Se multiplica por el color del sprite (el golpe recibido y la derrota lo respetan).")]
    public Color tinte = Color.white;
    [Tooltip("Marca los sprites de otro personaje prestados: deja un aviso en la consola al usarlo.")]
    public bool provisional;
    [Tooltip("Personaje con sprites propios: nunca cae en los de Abby. Sin una pose usa la que tenga (caminar o de pie) y lo que le falte al oeste lo voltea.")]
    public bool soloPropios;

    public Sprite[] idle = new Sprite[8];
    public AbbySpriteAnimator.Frames[] walk = new AbbySpriteAnimator.Frames[8];
    public AbbySpriteAnimator.Frames[] run = new AbbySpriteAnimator.Frames[8];
    public AbbySpriteAnimator.Frames[] dashFrames = new AbbySpriteAnimator.Frames[8];
    public AbbySpriteAnimator.Frames[] slash = new AbbySpriteAnimator.Frames[8];
    public AbbySpriteAnimator.Frames[] blockFrames = new AbbySpriteAnimator.Frames[8];
    public AbbySpriteAnimator.Frames[] hurt = new AbbySpriteAnimator.Frames[8];
    public AbbySpriteAnimator.Frames[] idleAnim = new AbbySpriteAnimator.Frames[8];
    public Sprite[] golpePose = new Sprite[8], guardia = new Sprite[8], rodilla = new Sprite[8], tendida = new Sprite[8];
}
