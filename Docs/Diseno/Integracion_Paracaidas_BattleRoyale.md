# Integración del paracaídas en Battle Royale

La escena `07_BattleRoyaleTest` ahora presenta un menú previo antes de iniciar el gameplay. La integración se crea en runtime para conservar los ajustes locales de la escena y reutiliza el avión, el paracaídas y el flujo aprobados en el punto 10.

## Flujo

1. Al entrar en Play Mode, el inicio automático del `DemoBootstrap` queda desactivado.
2. El jugador ve el menú previo y permanece sin control mientras está abierto.
3. `INICIAR PARTIDA` registra el pedido y comienza la cuenta regresiva.
4. El modelo 3D del avión usa la rotación local `X = -90°`, `Y = -90°`, `Z = 0°` dentro del prefab. La ruta de vuelo es independiente y recorre el mapa a una altura jugable.
5. Mientras el jugador está en el avión, la cámara usa 5.2 veces la distancia normal, se eleva y se alinea con la dirección del vuelo para mostrar el vehículo completo.
6. Al saltar, la cámara recupera su distancia normal y continúa el flujo de caída libre, apertura y aterrizaje.
7. La zona segura y el estado `Playing` comienzan después del aterrizaje.
8. Los avisos de daño por zona permanecen ocultos durante el menú, la cuenta regresiva y el vuelo.

## Prueba rápida

1. Abrir `Assets/_Game/Scenes/07_BattleRoyaleTest.unity`.
2. Iniciar Play Mode.
3. Confirmar que aparece el menú y que el jugador no se mueve detrás de él.
4. Pulsar `INICIAR PARTIDA`.
5. Verificar la vista abierta del avión y saltar con `F` o `Espacio`.
6. Abrir el paracaídas, aterrizar y confirmar que empieza el Battle Royale normal.

No es necesario guardar cambios en la escena para habilitar esta integración.
