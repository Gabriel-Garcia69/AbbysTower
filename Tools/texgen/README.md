# Generador de texturas de los pisos

Genera las texturas realistas (sin costuras, 512 px) de `Assets/Textures/Pisos/`:
`<tema>_floor|wall_albedo.png`, `_normal.png`, `_mask.png` (R metálico, A suavidad) y `_emission.png` (Ira).

```
pip install pillow numpy
python Tools/texgen/texgen.py Assets/Textures/Pisos
```

Después, en Unity, regenera el piso (FloorLayout → "Generar piso"). Si cambias una textura ya importada,
borra el material `Assets/Materials/Nivel/Superficie_<tema>_*.mat` para que se vuelva a crear.
