# Plan maestro de implementación — RulesOfSurvival_Go

Este documento es la referencia funcional principal del proyecto. Define el alcance que se implementará progresivamente y el orden recomendado de trabajo. No se considera un listado opcional: representa el conjunto de sistemas y funcionalidades previstas para completar la experiencia objetivo.

## 1. Muerte, eliminación y fin de partida

* Estado `Dead` para el jugador.
* Bloquear movimiento, salto, sprint, disparo, recarga e interacción al morir.
* Reproducir animación de muerte.
* Desactivar o cambiar el control de cámara después de morir.
* Restar al jugador del contador de vivos.
* Registrar al jugador que realizó la eliminación.
* Crear caja/cubo de loot al morir.
* Transferir inventario del jugador a la caja de loot.
* Mostrar pantalla de eliminación.
* Mostrar posición final del jugador.
* Detectar cuando queda un solo jugador vivo.
* Mostrar pantalla de victoria.
* Finalizar correctamente el estado de la partida.

## 2. Sistema de daño completo

* Daño por armas de fuego.
* Daño por explosiones.
* Daño por caída.
* Daño por zona segura.
* Diferentes zonas de impacto del cuerpo.
* Headshot.
* Torso.
* Brazos.
* Piernas.
* Multiplicadores de daño según parte del cuerpo.
* Sistema de casco.
* Sistema de chaleco.
* Reducción de daño según nivel de protección.
* Durabilidad de casco y chaleco.
* Indicador visual al recibir daño.
* Dirección desde donde llega el disparo.
* Hitmarker para quien dispara.
* Sonido de impacto.
* Efectos de sangre o impacto.

## 3. Loot e interacción

* Armas tiradas en el escenario.
* Munición.
* Medicamentos.
* Cascos.
* Chalecos.
* Mochilas.
* Accesorios para armas.
* Granadas.
* Objetos especiales.
* Sistema de objetos cercanos.
* Botón o tecla de interacción.
* Recoger objetos individualmente.
* Recoger objetos automáticamente si corresponde.
* Cambiar arma al recoger otra.
* Tirar objetos al suelo.
* Generación de loot en edificios.
* Puntos de spawn configurables.
* Rareza o probabilidad de aparición.
* Loot aleatorio por partida.

## 4. Inventario

* Inventario principal del jugador.
* Slots de arma primaria.
* Slot de arma secundaria.
* Slot de arma cuerpo a cuerpo.
* Slot de granadas.
* Slot de consumibles.
* Capacidad máxima del inventario.
* Capacidad según nivel de mochila.
* Mochila nivel 1.
* Mochila nivel 2.
* Mochila nivel 3.
* Objetos apilables.
* Separar stacks.
* Arrastrar y soltar objetos.
* Equipar y desequipar objetos.
* Tirar objetos.
* Tirar cantidades específicas.
* Intercambiar armas.
* Mostrar munición compatible.
* Mostrar accesorios equipados.
* Lootear cajas de jugadores eliminados.

## 5. Sistema de armas completo

* Rifles de asalto.
* Subfusiles.
* Francotiradores.
* Escopetas.
* Pistolas.
* Ametralladoras.
* Armas cuerpo a cuerpo.
* Diferentes tipos de munición.
* Daño individual por arma.
* Cadencia individual.
* Velocidad de proyectil.
* Alcance.
* Caída de bala.
* Spread.
* Recoil vertical.
* Recoil horizontal.
* Bloom.
* Disparo desde cadera.
* Disparo apuntando.
* Modo automático.
* Modo semiautomático.
* Modo ráfaga.
* Cambio de modo de disparo.
* Recarga normal.
* Recarga con cargador vacío.
* Diferentes tiempos de recarga.
* Animaciones individuales por arma.
* Sonidos individuales.
* Flash del cañón.
* Eyección de casquillos.
* Trazadoras.
* Impactos contra diferentes materiales.
* Accesorios de arma.
* Silenciador.
* Compensador.
* Empuñadura.
* Cargador extendido.
* Culata.
* Mira holográfica.
* Mira punto rojo.
* Mira 2x.
* Mira 4x.
* Mira 8x.

## 6. Consumibles

* Vendajes.
* Botiquín.
* Kit médico.
* Bebidas energéticas.
* Adrenalina u objetos similares.
* Curación parcial.
* Curación completa.
* Sistema de energía.
* Regeneración progresiva.
* Tiempo de uso por consumible.
* Barra de progreso.
* Animación de curación.
* Bloqueo parcial de acciones durante el uso.
* Cancelar uso al disparar.
* Cancelar uso al recibir daño si corresponde.
* Cancelar uso al moverse según el objeto.
* Cantidad disponible en HUD.
* Acceso rápido desde el HUD.

## 7. Paracaídas y comienzo de partida

* Sala previa a la partida.
* Inicio automático al completar jugadores.
* Avión recorriendo el mapa.
* Ruta del avión.
* Mostrar ruta en mapa.
* Permitir saltar desde el avión.
* Caída libre.
* Movimiento durante la caída.
* Rotación en caída libre.
* Velocidad vertical.
* Velocidad horizontal.
* Apertura manual del paracaídas.
* Apertura automática a determinada altura.
* Control del paracaídas.
* Animaciones de paracaídas.
* Sonidos del viento.
* Aterrizaje.
* Transición a locomoción normal.
* Soltar el paracaídas después de aterrizar.

## 8. Mapa y minimapa

* Minimapa en HUD.
* Rotación según orientación del jugador.
* Icono del jugador.
* Iconos de compañeros si existen.
* Safe Zone visible.
* Zona roja o zonas de peligro.
* Próximo círculo visible.
* Temporizador de zona.
* Mapa grande.
* Abrir y cerrar mapa.
* Zoom.
* Marcadores personalizados.
* Eliminar marcadores.
* Distancia al marcador.
* Nombre de ubicaciones.
* Brújula.
* Indicadores de disparos.
* Indicadores de pasos.
* Indicadores de vehículos.
* Indicadores de explosiones.

## 9. Vehículos

* Sistema base de vehículos.
* Entrar al vehículo.
* Salir del vehículo.
* Asiento del conductor.
* Asientos de pasajeros.
* Cambio de asiento.
* Conducción.
* Aceleración.
* Frenado.
* Reversa.
* Dirección.
* Freno de mano.
* Física de suspensión.
* Daño por colisión.
* Vida del vehículo.
* Explosión del vehículo.
* Daño a jugadores por explosión.
* Combustible.
* Consumo de combustible.
* Recarga de combustible.
* Sonido de motor.
* Claxon.
* Luces.
* Animaciones para entrar y salir.
* Cámara específica de vehículo.
* HUD de velocidad.
* HUD de combustible.
* Diferentes tipos de vehículos.

## 10. Mundo y escenario

* Terreno principal.
* Carreteras.
* Montañas.
* Ríos o zonas de agua.
* Vegetación.
* Árboles.
* Rocas.
* Casas.
* Edificios grandes.
* Interiores.
* Puertas.
* Ventanas.
* Escaleras.
* Muros.
* Cercas.
* Objetos de cobertura.
* Colisiones optimizadas.
* Puntos de interés.
* Distribución de loot.
* Spawns de vehículos.
* Spawns de jugadores.
* Materiales de terreno.
* Efectos ambientales.
* Iluminación.
* Ciclo día/noche si se desea.
* LOD.
* Occlusion Culling.
* Optimización del escenario.

## 11. Audio

* Pasos sobre tierra.
* Pasos sobre cemento.
* Pasos sobre madera.
* Pasos sobre metal.
* Pasos sobre agua.
* Saltos.
* Aterrizajes.
* Disparos.
* Recargas.
* Cambio de arma.
* Impactos.
* Headshots.
* Explosiones.
* Granadas.
* Vehículos.
* Motores.
* Claxon.
* Safe Zone.
* Daño.
* Curación.
* Muerte.
* UI.
* Inventario.
* Loot.
* Ambiente.
* Viento.
* Aves.
* Sonido 3D según distancia.
* Atenuación por distancia.
* Oclusión de sonido según obstáculos.

## 12. HUD completo

* Barra de vida.
* Barra de armadura.
* Estado del casco.
* Munición actual.
* Munición de reserva.
* Arma equipada.
* Modo de disparo.
* Crosshair.
* Indicador de recarga.
* Slots rápidos.
* Consumibles.
* Granadas.
* Brújula.
* Minimapa.
* Jugadores vivos.
* Eliminaciones.
* Kill Feed.
* Safe Zone.
* Temporizador.
* Indicador fuera de zona.
* Indicador de daño.
* Dirección del daño.
* Marcadores.
* Distancia a marcadores.
* Mensajes contextuales.
* Interacción con objetos.
* Estado del vehículo.
* Velocidad del vehículo.
* Combustible.
* Panel de inventario.
* Pantalla de muerte.
* Pantalla de victoria.
* Pantalla de resultados.

## 13. Bots / enemigos para pruebas

* IA básica.
* Detectar jugadores.
* Patrullaje.
* Buscar cobertura.
* Perseguir.
* Disparar.
* Recargar.
* Cambiar arma.
* Curarse.
* Recoger loot.
* Moverse hacia Safe Zone.
* Evitar quedarse fuera de zona.
* Reaccionar a disparos.
* Reaccionar a explosiones.
* Diferentes niveles de dificultad.
* Precisión configurable.
* Tiempo de reacción configurable.
* Bots suficientes para probar partidas completas.

## 14. Networking

* Conexión cliente-servidor.
* Sincronización de jugadores.
* Sincronización de posición.
* Sincronización de rotación.
* Sincronización de animaciones.
* Sincronización de disparos.
* Sincronización de recargas.
* Sincronización de daño.
* Sincronización de vida.
* Sincronización de muerte.
* Sincronización de loot.
* Sincronización de inventario.
* Sincronización de vehículos.
* Sincronización de Safe Zone.
* Sincronización del avión.
* Sincronización del paracaídas.
* Autoridad del servidor.
* Validación de disparos.
* Lag compensation.
* Reconexión.
* Manejo de desconexiones.
* Respawn únicamente en modos que lo permitan.
* Seguridad contra acciones inválidas.

## 15. Lobby y matchmaking

* Pantalla principal.
* Perfil del jugador.
* Nombre del jugador.
* Nivel.
* Estadísticas.
* Selección de modo.
* Solo.
* Dúo.
* Squad.
* Crear grupo.
* Invitar jugadores.
* Ready.
* Matchmaking.
* Cola de espera.
* Encontrar partida.
* Entrar al lobby previo.
* Contador para iniciar.
* Mostrar jugadores conectados.
* Cargar mapa.
* Iniciar partida.
* Resultado final.
* Regresar al lobby.

## 16. Optimización y pulido

* Object Pooling.
* Pooling de balas.
* Pooling de impactos.
* Pooling de partículas.
* Pooling de loot.
* LOD.
* Occlusion Culling.
* Batching.
* GPU Instancing.
* Optimización de materiales.
* Optimización de shaders.
* Optimización de luces.
* Optimización de sombras.
* Optimización de físicas.
* Optimización de colliders.
* Optimización de animaciones.
* Optimización del Animator.
* Optimización del networking.
* Reducción de tráfico de red.
* Pruebas de FPS.
* Pruebas de memoria.
* Pruebas con muchos jugadores.
* Quality Settings.
* Configuración gráfica.
* Resolución.
* Calidad de sombras.
* Distancia de dibujado.
* Sensibilidad.
* Controles.
* Corrección de bugs.
* Pulido visual.
* Pulido de animaciones.
* Pulido de sonido.
* Build final.

## Orden recomendado de implementación

1. Muerte, eliminación y fin de partida.
2. Caja de loot del jugador eliminado.
3. Sistema de daño completo.
4. Loot e interacción.
5. Inventario.
6. Sistema de armas completo.
7. Consumibles.
8. HUD completo.
9. Mapa y minimapa.
10. Paracaídas e inicio de partida.
11. Vehículos.
12. Mundo y escenario.
13. Audio.
14. Bots para pruebas.
15. Networking.
16. Lobby y matchmaking.
17. Optimización y pulido.
