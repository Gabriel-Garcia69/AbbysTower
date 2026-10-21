/// <summary>
/// GDD (casos especiales): si Abby muere durante el jefe, se reinicia el combate con el jefe, no el piso entero. Guarda
/// en qué escena empezó la pelea con Dante; FloorDirector, al recargar esa escena, limpia las hordas y la pone junto al jefe.
/// Se borra al terminar la demo o al volver al menú.
/// </summary>
public static class ReintentoJefe
{
    public static string Escena;
}
