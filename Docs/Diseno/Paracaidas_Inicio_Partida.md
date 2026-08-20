# Paracaídas e inicio de partida

Esta fase implementa el punto 10 del orden recomendado del plan maestro. La escena de prueba `06_ParachuteTest` utiliza los modelos 3D existentes de avión y paracaídas y no modifica la escena de Battle Royale en desarrollo.

## Flujo implementado

1. La partida entra en `Warmup` durante una cuenta regresiva corta.
2. El jugador se coloca en el avión y el estado cambia a `Plane`.
3. El avión recorre una ruta configurable de inicio a fin.
4. El jugador puede saltar con `F` o `Espacio`; si no salta, se fuerza el salto antes de terminar la ruta.
5. En caída libre se permite movimiento horizontal con `WASD`.
6. `Espacio` abre manualmente el paracaídas.
7. El paracaídas se abre automáticamente a 32 metros sobre el suelo, calculados mediante un raycast y no por altura absoluta del mundo.
8. Durante el planeo, `A/D` gira y `W/S` regula el avance.
9. El movimiento terrestre y las armas permanecen bloqueados mientras el jugador está en el avión o en el aire.
10. El Animator recibe la velocidad vertical del sistema aéreo y reutiliza las animaciones existentes de caída y aterrizaje.
11. Al tocar el suelo se oculta el paracaídas, vuelve la locomoción normal y la partida entra en `Playing`.

## Recursos usados

- Avión: `Assets/_Game/Art/Vehicles/Aircraft/Airplane_Starfighter/Airplane_Starfighter.fbx`.
- Paracaídas: `Assets/_Game/Art/Parachute/Models/Parachute.fbx`.
- Prefabs generados en `Assets/_Game/Resources/Parachute` para que el flujo de prueba pueda cargarlos en runtime.

## Prueba rápida

1. Ejecutar `ROS Battle Royale > Build Parachute Match Start` una sola vez si faltan los prefabs.
2. Abrir `Assets/_Game/Scenes/06_ParachuteTest.unity`.
3. Iniciar Play Mode.
4. Confirmar la cuenta regresiva y el recorrido del avión.
5. Saltar con `F`, abrir con `Espacio` y aterrizar.
6. Repetir sin abrir manualmente para comprobar la apertura automática.

La escena conserva su placeholder antiguo desactivado únicamente en runtime, por lo que no hace falta guardar cambios de escena para probar este punto.
