RULES OF SURVIVAL — DOCUMENTACIÓN FINAL CONSOLIDADA
Referencia integral del videojuego y guía de sistemas para recreación en Unity

Versión: consolidada y ampliada con los compendios complementarios proporcionados por el usuario.
Objetivo: reunir en un solo TXT la información histórica, jugable, técnica y de diseño disponible en el documento fuente, evitando duplicaciones y conservando las inconsistencias como notas de verificación.
IMPORTANTE SOBRE LOS DATOS
- Rules of Survival tuvo múltiples versiones, mapas, parches y cambios de balance.
- Parte de la información procede de una wiki comunitaria histórica y puede contener datos contradictorios, vandalizados o correspondientes a versiones diferentes.
- Cuando dos datos del material fuente no coinciden, este documento conserva ambos y los marca en la sección final de inconsistencias.
- Para una recreación fiel en Unity, los valores numéricos deben tratarse como referencia inicial hasta contrastarlos con capturas, vídeos o una build histórica del juego.
- Los valores de cadencia (RPM), recoil cualitativo y algunos cargadores añadidos desde el compendio extendido se mantienen en un bloque separado como ESTIMACIONES COMUNITARIAS; no sustituyen datos verificados.
ÍNDICE GENERAL
1. Información general del juego
2. Modos de equipo
3. Bucle de una partida
3A. Mecánicas avanzadas, controles móviles y HUD
4. Mapas
5. Sistema de zona segura
6. Supply Crates / Airdrops
7. Armas — resumen general
8. Armas cuerpo a cuerpo
9. Granadas y lanzables
10. Pistolas
11. Submachine Guns
12. Rifles / Assault Rifles / LMG
13. Shotguns
14. Sniper Rifles
15. Munición
16. Accesorios de armas
17. Equipamiento defensivo
18. Curación y suministros
19. Vehículos
20. Loot e inventario
21. Ranking comunitario de armas
21A. Sistema de rangos competitivo
22. Eventos y cambios visuales
22A. Economía y monedas
22B. Sistema de cosméticos
22C. Comunidad, esports y controversias históricas
23. Sistemas deducibles para Unity
24. Datos complementarios de armas y objetos
24A. Balance referencial estimado por arma (no oficial)
24B. Consejos tácticos por rol
25. Inconsistencias y puntos que deben verificarse
26. Checklist de fidelidad para una recreación en Unity

==============================================================================
1. INFORMACIÓN GENERAL DEL JUEGO
==============================================================================
Nombre: Rules of Survival | Desarrollador: NetEase Games | Editor: NetEase Games | Motor: Unity | Género: Battle Royale / shooter multijugador.
Plataformas históricas:
- Microsoft Windows.
- Android.
- iOS.
Cronología general:
- Acceso beta: noviembre de 2017.
- Lanzamiento global: 31 de mayo de 2018.
- En octubre de 2018 se reportaban aproximadamente 230 millones de jugadores.
- Los servidores cerraron el 27 de junio de 2022.
- Febrero de 2018: introducción de Fearless Fiord y de mecánicas asociadas como Ala Delta y minas.
- Entre 2018 y 2021 existieron ciclos de temporadas, pases, eventos y torneos comunitarios.
- Hubo versiones regionales y servidores separados por zonas geográficas.
Concepto:
Rules of Survival enfrenta a un gran número de jugadores en un mapa donde deben:
- Saltar desde un avión.
- Elegir una zona de aterrizaje.
- Buscar armas.
- Buscar munición.
- Conseguir armadura.
- Conseguir casco.
- Conseguir mochila.
- Recoger objetos de curación.
- Utilizar vehículos.
- Moverse hacia la zona segura.
- Eliminar a otros jugadores.
- Sobrevivir hasta ser el último jugador o equipo.

==============================================================================
2. MODOS DE EQUIPO
==============================================================================
Modos básicos mencionados:
- Solo: 1 jugador.
- Duo: 2 jugadores.
- Squad: 4 jugadores.
- Fireteam: 5 jugadores.
Otros modos referenciados históricamente:
- Gold Mode.
- Diamond Mode.
- Blitzkrieg.
Blitzkrieg:
- Asociado a Fearless Fiord.
- El jugador aterriza en una zona limitada.
- Comienza ya equipado con elementos básicos.
- Está planteado como un modo de enfrentamiento más inmediato.
Matchmaking / emparejamiento (referencia histórica del compendio complementario):
- Consideraba rango, región y latencia.
- En modos casuales el emparejamiento podía ser más flexible.
- En ranked se priorizaban skill/rango con mayor peso competitivo.

==============================================================================
3. BUCLE DE UNA PARTIDA
==============================================================================
Flujo general:
1. Los jugadores esperan antes de comenzar.
2. Se inicia el vuelo del avión.
3. Cada jugador elige cuándo saltar.
4. Desciende en paracaídas.
5. Aterriza sin equipamiento completo.
6. Busca loot.
7. Equipa armas, munición, protección y consumibles.
8. Se mueve según la zona segura.
9. Enfrenta a otros jugadores.
10. Puede saquear los objetos de jugadores eliminados.
11. Puede usar vehículos.
12. Recibe airdrops/supply crates durante la partida.
13. La zona jugable se reduce progresivamente.
14. Los jugadores fuera de la zona reciben daño.
15. Gana el último jugador o equipo con vida.
Perspectiva:
- Tercera persona.
- Posibilidad histórica de alternar a primera persona.
- Existieron modos dedicados a primera persona.
Lectura táctica del flujo de partida (referencia histórica/comunitaria):
- Early game: priorizar arma funcional, mochila, chaleco y recursos básicos.
- Mid game: controlar recursos, anticipar la zona y preparar rotaciones.
- Airdrops: alto valor y alto riesgo por concentración de jugadores.
- Late game: aumenta la importancia de cobertura, altura, humo y control de líneas de visión.

==============================================================================
3A. MECÁNICAS AVANZADAS, CONTROLES MÓVILES Y HUD
==============================================================================
Radar visual de sonido
- El HUD representaba visualmente sonidos cercanos, especialmente pasos, vehículos y disparos.
- La señal se mostraba en el minimapa/HUD superior.
- Color, intensidad y orientación ayudaban a inferir distancia y dirección de una amenaza.
- Este sistema era especialmente importante en móvil porque compensaba parcialmente las limitaciones del audio direccional en altavoces o auriculares simples.
Asistencia de apuntado (Aim Assist)
- Fundamental en la versión móvil.
- Al apuntar cerca de un enemigo, la mira podía recibir una corrección suave hacia el torso.
- Debe implementarse como asistencia gradual, no como un bloqueo instantáneo, si se busca reproducir la sensación original.
Peek & Fire / inclinación
- Opción configurable para asomarse a izquierda o derecha detrás de una cobertura.
- Expone principalmente cabeza, hombros y arma en lugar del cuerpo completo.
- Tiene importancia táctica en combates de nivel alto.
- Para Unity conviene separar LeanLeft, LeanRight y FireWhileLeaning del movimiento base.
Doble botón de disparo / técnica Claw
- El HUD móvil permitía disponer de un botón de disparo a la izquierda y otro a la derecha.
- Facilitaba mover, apuntar y disparar usando 3 o 4 dedos.
- La distribución del HUD debía poder personalizarse para distintas formas de control táctil.
Elementos de HUD que deben contemplarse
- Minimap y brújula.
- Indicadores direccionales de disparos, pasos y vehículos.
- Vida, armadura, casco y mochila.
- Arma activa, arma secundaria y munición actual/reserva.
- Crosshair y visor óptico.
- Jugadores vivos.
- Kill feed.
- Estado del equipo.
- Botones de interacción.
- Inventario rápido y ventana de loot.
- Temporizador de zona segura y visualización de círculo actual/siguiente.
- UI específica de vehículos.
- Botones móviles configurables para movimiento, cámara, disparo, ADS, salto, agacharse, inclinarse, recargar, cambiar arma e interactuar.
Ajustes y señales adicionales recuperadas:
- Indicadores de daño/dirección de disparo mediante flechas o marcadores.
- Barra/estado de energía para boosters y regeneración.
- Layout táctil personalizable: posición y tamaño de botones según preferencias del jugador.
- Sensibilidad diferenciada para hipfire, ADS y miras de distintos aumentos.
- Movimiento asociado: sprint, crouch, prone, salto y superación/trepa de obstáculos bajos.
- Fearless Fiord añadió desplazamiento contextual mediante ziplines y Ala Delta.

==============================================================================
4. MAPAS
==============================================================================

[4.1 GHILLIE ISLAND]
Mapa inicial/clásico.
Características generales:
- Hasta 120 jugadores.
- Tamaño aproximado citado externamente: 4.8 km x 4.8 km.
- La wiki indica 15 ubicaciones principales.
- Contiene edificios, zonas abiertas, almacenes, zonas industriales y puntos de loot.
- La pequeña isla del norte forma parte de la geografía del mapa.
Nombres de ubicaciones recuperados de la wiki y guías históricas:
- Bitter Lake.
- Rust Bay.
- Training Base.
- Research Edifice.
- Observatory.
- Wheat Town.
- Echo Valley.
- Chemical Depot.
- Logging Camps.
- Squirrel Depot.
- Laboratory.
- Warehouses.
- Masout Factory.
Notas:
- Bitter Lake fue una de las zonas de aterrizaje más populares.
- Rust Bay también concentra edificios y loot.
- Training Base es una zona relativamente abierta.
- Research Edifice destaca por una gran estructura/pirámide.
- Observatory contiene varios edificios.
- Wheat Town contiene múltiples construcciones pequeñas.
DATOS COMPLEMENTARIOS DE GHILLIE ISLAND
- Una referencia del material la describe como aproximadamente 4 x 4 km, mientras otra la sitúa en aproximadamente 4.8 x 4.8 km. Tratar el tamaño exacto como dato por verificar.
- Safe Store aparece mencionada como zona industrial/noreste con almacenes y equipo militar.
- Bitter Lake se describe como una zona central de loot alto y combate frecuente.
- Rust Bay se asocia a puerto, contenedores y saqueo rápido.
- Training Base se asocia a loot militar y alta probabilidad de rifles/armas de largo alcance.
- Research Edifice destaca por un gran complejo/estructura singular.
- Observatory se encuentra en zona elevada y favorece líneas de visión largas.

[4.2 FEARLESS FIORD / FEARLESS FJORD]
Segundo gran mapa.
Características históricas:
- Hasta 300 jugadores en Solo.
- Aproximadamente 64 km².
- Aproximadamente 8 km x 8 km.
- Introdujo terrenos adicionales.
- Incluye zonas de pantano.
- Incluye jardines.
- Incluye minas.
- Incluye ziplines.
- Amplía el combate terrestre, aéreo y sobre agua.
Elementos particulares:
- Minas antipersona.
- Minas antivehículo.
- Las minas tienen una zona de activación.
- Pueden dañar jugadores y/o vehículos.
- Algunas posiciones podían verse en el mapa.
- Ziplines para desplazamiento rápido.
Vehículos introducidos/relevantes:
- Hang Glider.
- Hovercraft.
- Truck.
Capacidades históricas reportadas:
- Hang Glider: hasta 5 jugadores.
- Hovercraft: hasta 5 jugadores.
- Truck: hasta 6 jugadores.
Armas agregadas/asociadas a Fearless Fiord:
- ACR.
- AN94.
- Vector.
- MP5.
- P90.
- QBU88.
- M110.
- SAIGA-12.
- WRO.
- RPG.
PUNTOS DE INTERÉS COMPLEMENTARIOS DE FEARLESS FIORD
- Area 42: base militar secreta, descrita como zona de alto riesgo y recompensa.
- Traffic Hub: gran estación o nodo central de tránsito.
- Fortune Island: isla con buen loot pero riesgo elevado de quedar aislado si la zona se desplaza.
- Hillyland: terreno montañoso donde los vehículos off-road son especialmente útiles.
- El mapa fue asociado a una expansión de movilidad: tierra, agua, aire, minas y ziplines.
Diseño de distribución / POI (referencia complementaria):
- Los POI pueden modelarse por niveles de loot (bajo, medio, alto) para controlar probabilidad/calidad de objetos.
- La ruta del avión modifica la densidad inicial de jugadores y el riesgo efectivo de cada POI.
- Los airdrops también alteran temporalmente las rutas y zonas de conflicto.

==============================================================================
5. SISTEMA DE ZONA SEGURA
==============================================================================
Mecánica central del Battle Royale:
- La zona jugable se reduce durante la partida.
- Los jugadores fuera del área segura reciben daño.
- La reducción obliga a los jugadores restantes a acercarse.
- La progresión aumenta la probabilidad de enfrentamientos.
Implementación recomendada para Unity:
- SafeZoneController.
- Fases configurables.
- Radio inicial y final.
- Tiempo de espera.
- Tiempo de cierre.
- Daño por segundo fuera de zona.
- Posición aleatoria o semialeatoria del siguiente círculo.
- UI para círculo actual y siguiente.
- Temporizador de cierre.

==============================================================================
6. SUPPLY CRATES / AIRDROPS
==============================================================================
Durante una partida pueden aparecer cajas de suministro.
Características:
- Contienen objetos poco comunes o exclusivos.
- Algunas armas solo aparecen en Supply Crates.
- Son puntos de alto riesgo porque atraen jugadores.
Armas señaladas como exclusivas o asociadas a cajas:
- M249.
- Barrett.
- AS VAL.
- AUG.
- Cardio Tonic como consumible de supply crate.
Nota: Algunas fichas comunitarias son contradictorias respecto a la disponibilidad exacta de M14EBR y otros objetos.
CONTENIDO COMPLEMENTARIO DE AIRDROPS
- Traje Ghillie: camuflaje de hierba destinado a reducir la visibilidad del jugador en terreno abierto/vegetación.
- Equipo de Nivel 3 aparece descrito como contenido de alta probabilidad o garantizado en referencias del material.
- RPG aparece asociado a supply drops o a modos/versiones concretas.
- Las cajas generan zonas de conflicto porque el humo y la ruta del avión revelan su posición aproximada a varios jugadores.

==============================================================================
7. ARMAS — RESUMEN GENERAL
==============================================================================
Categorías recuperadas:
- Melee.
- Throwables / Grenades.
- Pistols.
- Submachine Guns.
- Assault Rifles / Rifles.
- Shotguns.
- Sniper Rifles.
- Light Machine Gun.
- RPG.

==============================================================================
8. ARMAS CUERPO A CUERPO
==============================================================================
Elementos recuperados:
- Fists.
- Claw.
- Damascus Knife.
- Frying Pan.
- Crowbar.
- Rubber Chicken.
Fists:
- Arma por defecto cuando no se tiene otra equipada.
Claw:
- Asociada a Zombies.
- La wiki indica que un golpe directo a la cabeza podía ser letal.
Damascus Knife:
- La página general la describe con velocidad de ataque elevada frente a otros objetos melee.
Frying Pan:
- Puede actuar como objeto defensivo frente a proyectiles según la descripción comunitaria.
Crowbar:
- La página general le atribuye alto daño entre armas melee.
Rubber Chicken:
- Aumenta la velocidad de movimiento al llevarlo.
- La wiki también menciona capacidad de desviar proyectiles al recibir impactos.
Efecto general indicado para melee:
- Al equiparlos pueden reducir ciertas señales visuales de audio.
- Pueden aumentar la velocidad de carrera.

==============================================================================
9. GRANADAS Y LANZABLES
==============================================================================
Lista recuperada:
- Stun Grenade.
- Smoke Grenade.
- Grenade.
- Molotov / Molotov Cocktail.
- Chicken Grenade.
- RPG.
Stun Grenade:
- Ciega/desorienta.
- Reduce temporalmente la capacidad auditiva/visual del enemigo.
Smoke Grenade:
- Genera humo.
- Reduce visibilidad.
- Útil para revivir, rotar o bloquear líneas de visión.
Grenade:
- Explosión de área.
- Daño elevado cerca del centro de explosión.
Molotov:
- Crea una zona de fuego.
- Aplica daño a jugadores dentro del área.
Chicken Grenade:
- Variante especial de granada.
- La wiki comunitaria la describe como una versión de mayor daño.
RPG:
- Ocupa un slot de arma.
- Dispara un cohete.
- Produce daño de área.
- Fue añadido con contenido asociado a Fearless Fiord.

==============================================================================
10. PISTOLAS
==============================================================================

[10.1 DESERT EAGLE]
Tipo: Pistol. | Ranking comunitario: D. | Munición: Pistol Ammo. | Cargador: 7. | Cargador extendido: N/A según la ficha indexada. | Modo: Single. | Accesorios: La ficha recuperada indica que no utiliza accesorios.

[10.2 G18C]
Aparece referenciada en:
- Tabla de Pistol Ammo.
- Ranking comunitario.
La wiki recuperada no ofrece una ficha completa indexada para esta extracción.

==============================================================================
11. SUBMACHINE GUNS
==============================================================================
Armas listadas:
- PP19.
- MP7.
- Thompson.
- Vector.
- MP5.
- P90.
Características de clase:
- Orientadas a combate cercano.
- Alta cadencia.
- Recoil generalmente menor que rifles.
- Menor daño por impacto que rifles.

[11.1 PP19]
Tipo: Submachine Gun. | Ranking comunitario: F. | Disponibilidad: Everywhere. | Munición: SMG Ammo. | Accesorios: 3. | Cargador: 25. | Cargador extendido: 35. | Modo: Single & Auto.
Accesorios asociados:
- SMG Silencer.
- SMG Compensator.
- SMG Smoke Hider.
- Triangle Grip.
- Vertical Foregrip.
- SMG Ex-Mag.
- SMG QD-Mag.
- SMG Ex-QD-Mag.
- Red Dot Sight.
- Holo Sight.
- 2x según la ficha recuperada.

[11.2 MP7]
Tipo: Submachine Gun. | Ranking comunitario: D. | Disponibilidad: Everywhere. | Munición: SMG Ammo. | Accesorios: 4. | Cargador: 30. | Cargador extendido: 40.
Accesorios recuperados:
- SMG Silencer.
- SMG Compensator.
- SMG Smoke Hider.
- Triangle Grip.
- Vertical Foregrip.
- SMG Ex-Mag.
- SMG QD-Mag.
- SMG Ex-QD-Mag.
- Red Dot Sight.
- Holo Sight.

[11.3 THOMPSON]
Tipo: Submachine Gun. | Ranking comunitario: C en su ficha antigua; el ranking global comunitario la coloca en B. | Disponibilidad: Everywhere. | Munición: SMG Ammo. | Accesorios: 3. | Cargador: 45. | Cargador extendido: 60. | Modo: Single & Auto.
Accesorios:
- SMG Silencer.
- SMG Compensator.
- SMG Smoke Hider.
- Triangle Grip.
- Vertical Foregrip.
- SMG Ex-Mag.
- SMG QD-Mag.
- SMG Ex-QD-Mag.

[11.4 VECTOR]
Añadida con Fearless Fiord.
Descripción histórica:
- SMG de alta estabilidad.
- Alta cadencia.
- Daño relativamente bajo.
- Cargador pequeño.
- Se beneficia especialmente de un cargador extendido.

[11.5 MP5]
Añadida con Fearless Fiord.
Descripción histórica:
- Mayor cadencia base que MP7.
- Mejor estabilidad.
- Mejor potencia general que MP7 según las notas del parche histórico.

[11.6 P90]
Añadida con Fearless Fiord.
Descripción:
- SMG equilibrada.
- Diseñada para rendir bien sin depender de demasiados accesorios.

==============================================================================
12. RIFLES / ASSAULT RIFLES
==============================================================================
Armas recuperadas:
- M4A1.
- AR15.
- AKM.
- M14EBR.
- M249.
- ACR.
- AN94.
- AUG.

[12.1 M4A1]
Tipo: Assault Rifle. | Ranking de ficha: B. | Disponibilidad: Everywhere. | Munición: Rifle Ammo. | Accesorios: 5 grupos. | Cargador: 30. | Cargador extendido: 40. | Modo: Single & Auto. | Particularidad: Es la ficha que permite el conjunto más amplio de accesorios entre los rifles antiguos.
Accesorios:
Muzzle:
- Rifle Silencer.
- Rifle Compensator.
- Rifle Flash Hider.
Grip:
- Triangle Grip.
- Vertical Foregrip.
Magazine:
- Rifle QD-Mag.
- Rifle Ex-Mag.
- Rifle Ex-QD-Mag.
Stock:
- M4 Stock / Tactical Stock.
Scopes:
- Red Dot Sight.
- Holo Sight.
- 2x.
- 4x.
- 8x.
Daño de tabla comunitaria:
- Cabeza: 46-102.
- Cuerpo: 18-41.
- Extremidades: 20.

[12.2 AR15]
Tipo: Assault Rifle. | Ranking de ficha: A. | Ranking global comunitario posterior: C. | Disponibilidad: Everywhere. | Munición: Rifle Ammo. | Accesorios: 4. | Cargador: 30. | Cargador extendido: 40. | Modo: Single & Auto.
Daño de tabla comunitaria:
- Cabeza: 46-102.
- Cuerpo: 18-41.
- Extremidades: 20.
Accesorios:
- Rifle Silencer.
- Rifle Compensator.
- Rifle Flash Hider.
- Triangle Grip.
- Vertical Foregrip.
- Rifle QD-Mag.
- Rifle Ex-Mag.
- Rifle Ex-QD-Mag.
- Red Dot.
- Holo.
- 2x.
- 4x.
- 8x.

[12.3 AKM]
Tipo: Assault Rifle. | Ranking de ficha: A. | Ranking global comunitario: S. | Disponibilidad: Everywhere. | Munición: Rifle Ammo. | Accesorios: 3. | Cargador: 30. | Cargador extendido: 40. | Modo: Single & Auto.
Daño de tabla comunitaria:
- Cabeza: 67-148.
- Cuerpo: 25-55.
- Extremidades: 27.
Características:
- Daño alto.
- Recoil elevado.
- Más difícil de controlar en automático.
- Mejora con compensador y uso a disparo único a distancia.

[12.4 M14EBR]
Tipo: Assault Rifle / marksman-like rifle. | Ranking: S. | Munición: Rifle Ammo. | Accesorios: 3. | Cargador: 30. | Cargador extendido: 40.
Modo:
- Single.
- 3-Round Burst.
Daño de tabla:
- Cabeza: 46-102.
- Cuerpo: 18-41.
- Extremidades: 20.
NOTA DE INCONSISTENCIA:
- Una parte de la ficha la marca como Supply Crate.
- Otra descripción de la misma página indica que puede encontrarse por el mapa.
- Tratar la distribución de spawn como dato no definitivo.

[12.5 M249]
Tipo oficial: Light Machine Gun. | Clasificación antigua de wiki: Assault Rifle no oficial. | Ranking: B en ficha antigua.
A en ranking comunitario posterior.
Disponibilidad: Supply Crates. | Munición: Rifle Ammo. | Accesorios: 1 grupo principal. | Cargador: 100. | Cargador extendido: N/A. | Modo: Single & Auto.
Accesorios:
Solo ópticas:
- Red Dot.
- Holo.
- 2x.
- 4x.
- 8x.

[12.6 ACR]
Introducida con Fearless Fiord.
Descripción:
- Rifle similar al AR15.
- La wiki indica que reemplazó al AR15 en ciertas versiones/mapas.
- Mejor control de recoil que AR15 según la descripción global.
Tabla comunitaria:
- Modo: Single & Auto.
- Accesorios: 4.
- Cargador: 30.
- Cabeza: 46-102.
- Cuerpo: 18-41.
- Extremidades: 20.

[12.7 AN94]
Introducida con Fearless Fiord.
Características:
- Rifle ruso de calibre pequeño.
- Buena precisión en ráfaga.
- Soporta automático y single.
- En single, el daño era descrito como segundo después del AKM.
Tabla comunitaria:
- Accesorios: 3.
- Cargador: 30.
- Cabeza: 52-115.
- Cuerpo: 22-48.
- Extremidades: 24.

[12.8 AUG]
Características:
- Solo Supply Crates según la página general.
- Single & Auto.
- Compatible con accesorios similares a AR15/ACR.
- Daño descrito como cercano al AKM.
Tabla comunitaria:
- Accesorios: 4.
- Cargador: 30.
- Cabeza: 46-102.
- Cuerpo: 18-41.
- Extremidades: 20.

==============================================================================
13. SHOTGUNS
==============================================================================
Lista:
- M1887.
- M870.
- AA12.
- SAIGA-12.
- WRO.

[13.1 M1887]
Tipo: Shotgun. | Ranking de ficha: B. | Ranking global comunitario: C. | Disponibilidad: Everywhere. | Munición: SG Ammo. | Accesorios: 2. | Cargador: 2. | Extendido: N/A. | Modo: Single & Auto según la ficha comunitaria.
Accesorios:
- SG Choke.
- SG Belt Loop.

[13.2 M870]
Tipo: Shotgun. | Ranking: C en ficha; B en ranking comunitario posterior. | Disponibilidad: Everywhere. | Munición: SG Ammo. | Cargador: 5. | Extendido: N/A. | Modo: Single.
Accesorios:
- SG Choke.
- SG Belt Loop.

[13.3 AA12]
Tipo: Shotgun. | Ranking: B. | Disponibilidad: Everywhere.
Características:
- Escopeta automática.
- La descripción general histórica indica cargador base de 5.
- Acepta cargadores de Rifle.
- Puede usar:
- Rifle QD-Mag.
- Rifle Ex-Mag.
- Rifle Ex-QD-Mag.
- No utiliza SG Bullet Loop o SG Choke según la página de accesorios.
NOTA: Hay resultados indexados que no coinciden completamente con la descripción global sobre capacidad. Debe verificarse antes de fijar valores finales.

[13.4 SAIGA-12]
Introducida con Fearless Fiord.
Características:
- Semi-automática.
- Compatible con muzzle.
- Compatible con ópticas de baja potencia.
- Diseñada para mejorar estabilidad.
- La wiki general la sitúa entre las escopetas de mayor daño.

[13.5 WRO]
Introducida con Fearless Fiord.
Características:
- Un solo disparo base.
- Alto daño.
- Mayor alcance que otras shotguns.
- Muy dependiente de SG Bullet Loop.
- SG Choke aumenta mucho su precisión.
- Puede funcionar a media distancia.

==============================================================================
14. SNIPER RIFLES
==============================================================================
Lista:
- AWM.
- SVD.
- Barrett.
- AS VAL.
- QBU88.
- M110.
La wiki también presenta errores tipográficos históricos:
- M1110 en lugar de M110.
- OBU88 en lugar de QBU88.

[14.1 AWM]
Tipo: Sniper Rifle. | Ranking: S en ficha. | Disponibilidad: Everywhere según la ficha recuperada. | Munición: SR Ammo. | Accesorios: 4. | Cargador: 5. | Extendido: 10. | Modo: Single.
Óptica:
- 4x por defecto según la tabla de clase.
- Puede aceptar otras ópticas.
Accesorios:
Muzzle:
- SR Silencer.
- SR Compensator.
- SR Flash Hider.
Magazine:
- SR Ex-Mag.
- SR QD-Mag.
- SR Ex-QD-Mag.
Stock:
- SR Cheek Pad.
Scopes:
- Red Dot.
- Holo.
- 2x.
- 4x.
- 8x.

[14.2 SVD]
Tipo: Sniper Rifle. | Ranking: B. | Disponibilidad: Everywhere. | Munición: SR Ammo. | Accesorios: 4. | Cargador: 10. | Extendido: 20. | Modo: Single en ficha; la descripción general la presenta como semiautomática. | Óptica: Holo por defecto en la tabla general.
Accesorios:
- SR Silencer.
- SR Compensator.
- SR Flash Hider.
- SR Ex-Mag.
- SR QD-Mag.
- SR Ex-QD-Mag.
- SR Cheek Pad.
- Red Dot.
- Holo.
- 2x.
- 4x.
- 8x.

[14.3 BARRETT]
Tipo: Sniper Rifle. | Ranking: S. | Disponibilidad: Supply Drops. | Munición: SR Ammo. | Accesorios: 4. | Cargador: 5. | Extendido: 10. | Modo: Single.
Características:
- Muy alto daño.
- Rifle calibre .50 en referencia real.
- Diseñado como arma de alto impacto.

[14.4 AS VAL]
Clasificación de wiki: Sniper Rifle aunque su funcionamiento se parece a un rifle automático. | Disponibilidad: Supply Crates. | Munición: SR Ammo. | Cargador: 20. | Extendido: 30. | Modo: Single & Auto.
Características:
- Supresor integrado.
- 4x integrada/referenciada.
- Accesorios no desmontables en parte de su configuración.
- Añadida en actualización del 28 de diciembre de 2017.

[14.5 QBU88]
Añadida con Fearless Fiord.
Características:
- Semi-automática.
- Mayor daño que SVD.
- Mayor recoil que SVD.
- El recoil puede mejorar con grip.

[14.6 M110]
Añadida con Fearless Fiord.
Características:
- Bolt-action.
- Incluye 4x.
- La nota de parche histórica indicaba mayor daño a la cabeza que AWM.

==============================================================================
15. MUNICIÓN
==============================================================================
La página "Ammunition" contiene valores de "Capacity".
Es probable que representen coste/peso de inventario por unidad o grupo,
por lo que deben comprobarse dentro del juego antes de usarlos como valor definitivo.
Rifle Ammo:
- Capacity: 0.25.
- Usada por la tabla antigua en:
- M4A1.
- AR15.
- AKM.
- M249.
- M14EBR.
SMG Ammo:
- Capacity: 0.2.
- Usada por:
- PP19.
- MP7.
- Thompson.
Pistol Ammo:
- Capacity: 0.6.
- Usada por:
- G18C.
- Desert Eagle.
SG Ammo:
- Capacity: 0.5.
- Usada por:
- M870.
- AA12.
- M1887.
SR Ammo:
- Capacity: 0.5.
- Usada por:
- AWM.
- SVD.
- Barrett.
- AS VAL.
NOTA: La tabla de Ammunition parece anterior a la incorporación de varias armas de Fearless Fiord,
por eso no incluye necesariamente ACR, AN94, AUG, Vector, MP5, P90, etc.

==============================================================================
16. ACCESORIOS DE ARMAS
==============================================================================

[16.1 MUZZLE — RIFLE]
- Rifle Silencer.
- Rifle Compensator.
- Rifle Flash Hider.
ATENCIÓN: La página de Fandom dedicada a Attachments tiene texto vandalizado en algunas filas de Rifle Muzzle.
No utilizar esas descripciones vandalizadas como datos de diseño.

[16.2 MUZZLE — SMG]
- SMG Silencer.
- SMG Compensator.
- SMG Smoke Hider.
Efectos recuperados:
SMG Compensator:
- Reduce recoil horizontal.
- Reduce recoil vertical.
- Reduce ligeramente la dispersión.
SMG Smoke Hider:
- Elimina/reduce muzzle flash.
- Reduce ligeramente recoil.

[16.3 MUZZLE — SHOTGUN]
SG Choke:
- Reduce dispersión de perdigones.
- La wiki indica aumento de cadencia en ciertos datos antiguos.
Compatibilidad clásica:
- M1887.
- M870.

[16.4 MUZZLE — SNIPER]
SR Silencer:
- Reduce ruido.
- Reduce dispersión según la ficha.
SR Compensator:
- Reduce recoil horizontal.
- Reduce recoil vertical.
- Reduce ligeramente dispersión.
SR Flash Hider:
- Oculta muzzle flash.
- Reduce ligeramente recoil.

[16.5 GRIPS]
Triangle Grip:
- Reduce ligeramente recoil horizontal/vertical.
- Mejora velocidad de abrir mira según la wiki.
Vertical Foregrip:
- Reduce recoil horizontal/vertical de forma más directa.
Compatibilidad clásica:
- M4A1.
- AR15.
- AKM.
- PP19.
- MP7.
- Thompson.
Nota: La wiki antigua indica que M14EBR no utiliza grip.

[16.6 MAGAZINES — RIFLE]
Rifle QD-Mag:
- Mejora velocidad de recarga.
Rifle Ex-Mag:
- Aumenta capacidad.
Rifle Ex-QD-Mag:
- Aumenta capacidad.
- Mejora recarga.
Compatibilidad clásica:
- M4A1.
- AR15.
- AKM.
- M14EBR.
- AA12.

[16.7 MAGAZINES — SMG]
SMG QD-Mag:
- Reduce tiempo de recarga.
SMG Ex-Mag:
- Aumenta capacidad.
SMG Ex-QD-Mag:
- Aumenta capacidad.
- Mejora recarga.

[16.8 MAGAZINES — SNIPER]
SR QD-Mag:
- Reduce tiempo de recarga.
SR Ex-Mag:
- Aumenta capacidad.
SR Ex-QD-Mag:
- Aumenta capacidad.
- Mejora recarga.
Compatibilidad:
- AWM.
- SVD.
- Barrett.
- AS VAL.

[16.9 STOCKS]
M4 Stock / Tactical Stock:
- Exclusivo de M4A1 en la tabla clásica.
- Reduce recoil.
SG Bullet Loop:
- Mejora recarga de escopetas.
- Compatible con M1887 y M870.
- WRO también depende de bullet belt/loop según contenido posterior.
SR Cheek Pad:
- Reduce recoil vertical.
- Mejora velocidad de recarga.

[16.10 SCOPES]
- Red Dot Sight.
- Holo Sight.
- 2x Scope.
- 4x Scope.
- 8x Scope.
Reglas antiguas:
- Shotguns tradicionales no usan scopes.
- SMGs antiguas tienen limitaciones de óptica.
- Thompson aparece con limitaciones particulares.
- Snipers traen una óptica por defecto según el arma.

==============================================================================
17. EQUIPAMIENTO DEFENSIVO
==============================================================================

[17.1 HELMETS]
Level 1 Helmet:
- 30% Damage Reduction.
Level 2 Helmet:
- 40% Damage Reduction.
Level 3 Helmet:
- 55% Damage Reduction.

[17.2 BODY ARMOR]
Level 1 Armor:
- 30% Damage Reduction.
- +25 Capacity.
Level 2 Armor:
- 40% Damage Reduction.
- +50 Capacity.
Level 3 Armor:
- 55% Damage Reduction.
- +50 Capacity.

[17.3 BACKPACKS]
Level 1 Backpack:
- +150 Capacity.
Level 2 Backpack:
- +200 Capacity.
Level 3 Backpack:
- +250 Capacity.

==============================================================================
18. OBJETOS DE CURACIÓN Y SUMINISTROS
==============================================================================
La página de Supplies indica que la mayoría de los objetos tardan aproximadamente
5 segundos en aplicarse.

[18.1 BANDAGE]
Efecto:
- +10 HP.
Eventos visuales históricos:
- En Navidad se retexturizó como Apple.
- En San Valentín se retexturizó como Rose.

[18.2 MED KIT]
Efecto:
- +50 HP.

[18.3 FIRST AID KIT]
Efecto:
- Restaura HP completo según la página comunitaria.

[18.4 SPORTS DRINK]
Efecto:
- +40 HP.
- Speed Boost temporal.
Duración del boost:
- Aproximadamente 2 minutos.

[18.5 CARDIO TONIC]
Efecto:
- +75 HP.
- Speed Boost temporal.
Disponibilidad:
- Supply Crates.
Duración del boost:
- Aproximadamente 3 minutos.

[18.6 FUEL BARREL]
Uso:
- Combustible para vehículos.
Mecánica:
- Los vehículos consumen combustible.
- Puede ser importante para rotaciones largas hacia la zona segura.

==============================================================================
19. VEHÍCULOS
==============================================================================
La wiki específica recuperada no contiene una ficha completa de todos los vehículos,
pero sí confirma el uso de combustible y existen referencias históricas adicionales.
Elementos conocidos:
- Cars.
- Motorcycles.
- Trucks.
- Vans.
- Hovercraft.
- Hang Glider.
Fearless Fiord agregó claramente:
- Hang Glider: hasta 5.
- Hovercraft: tierra y agua, hasta 5.
- Truck: tierra, hasta 6.
Sistema recomendado en Unity: VehicleController
- Acceleration.
- Brake.
- Steering.
- MaxSpeed.
- Fuel.
- VehicleHealth.
- Seats.
- DriverSeat.
- PassengerSeats.
- Enter/Exit.
- Damage.
- Explosion.
- Audio.
- Suspension.
- Surface handling.
CATÁLOGO COMPLEMENTARIO DE VEHÍCULOS MENCIONADOS
- Bicicleta: silenciosa, sin consumo de combustible en la descripción histórica y relativamente frágil.
- Motocicleta: muy rápida, normalmente para 2 jugadores y sensible a vuelcos/accidentes.
- Tuk-Tuk / trimoto: vehículo icónico, asociado a capacidad para 3 jugadores.
- Coche deportivo: rápido en carretera.
- Buggy: movilidad rápida y ligera, especialmente útil fuera de vías principales.
- SUV / Jeep / todoterreno: más lento, resistente y apropiado para escuadrones.
- Pickup / camioneta.
- Truck / camión: Fearless Fiord; referencia de hasta 6 jugadores.
- Hovercraft / aerodeslizador: anfibio, tierra y agua; referencia de hasta 5 jugadores.
- Hang Glider / ala delta: movilidad aérea; referencia de hasta 5 jugadores.
- Lancha rápida.
- Bote de pasajeros.
Comportamiento físico y táctico descrito en el material complementario:
- Los vehículos pueden recibir daño por impactos/colisiones y llegar a explotar.
- Motos, Tuk-Tuk y otros vehículos ligeros son más sensibles a vuelcos.
- Algunos vehículos consumen combustible; la bicicleta se describe como excepción sin consumo.
- Funcionan como cobertura parcial durante un enfrentamiento, aunque no deben considerarse protección total.
- Uso táctico: rotaciones rápidas entre círculos, aproximación/escape y cobertura temporal.
- En zonas cerradas conviene abandonar vehículos ruidosos para evitar revelar posición.
Skins de vehículos mencionadas
- Tuk-Tuk con apariencia de carruaje.
- Coche deportivo con variantes futuristas.
- Bicicleta/piezas cosméticas con temática de pollo.

==============================================================================
20. LOOT E INVENTARIO
==============================================================================
Sistema de loot:
- Armas dispersas por el mapa.
- Munición.
- Accesorios.
- Equipamiento.
- Curación.
- Combustible.
- Objetos de supply crates.
- Loot de enemigos eliminados.
Sistema de capacidad:
- Backpacks aumentan capacidad.
- Body Armor también aumenta cierta capacidad según la wiki.
- La munición consume capacidad.
- Los objetos de curación tienen peso/coste implícito.
Modelo recomendado para Unity: ItemDefinition
- Id.
- Name.
- Category.
- Weight/CapacityCost.
- MaxStack.
- Icon.
- WorldPrefab.
- PickupPrefab.
- Rarity.
InventoryComponent
- MaxCapacity.
- CurrentCapacity.
- Slots.
- Stack handling.
- Auto pickup.
- Drop.
- Split stack.
- Equip.
- Unequip.

==============================================================================
21. RANKING COMUNITARIO DE ARMAS
==============================================================================
IMPORTANTE: Este ranking NO es una clasificación oficial.
Es una opinión/meta de la wiki en una etapa concreta del juego.
S Rank:
- M14EBR.
- Barrett.
- AUG.
- AN94.
- AKM.
- Rubber Chicken.
A Rank:
- M249.
- M4A1.
B Rank:
- AA12.
- M870.
- AS VAL.
- Thompson.
- SVD.
- ACR.
- SAIGA-12.
C Rank:
- WRO.
- M1887.
- AR15.
D Rank:
- MP7.
- Desert Eagle.
F Rank:
- Crowbar.
- PP19.
F-:
- Fists.
- G18C.

==============================================================================
21A. SISTEMA DE RANGOS COMPETITIVO
==============================================================================
Los jugadores ascendían acumulando puntos en partidas clasificatorias.
Orden de rangos recuperado, de menor a mayor:
1. Bronze: I, II, III.
2. Silver: I, II, III, IV.
3. Gold: I, II, III, IV.
4. Platinum: I, II, III, IV, V.
5. Diamond: I, II, III, IV, V.
6. Grandmaster / Maestro.
7. Supreme: reservado para jugadores de la parte superior del servidor dentro del nivel competitivo más alto.
Para una recreación en Unity
- RankDefinition: nombre, divisiones, icono, límites de puntos y recompensas.
- RankedMatchResult: posición final, eliminaciones, daño, supervivencia y modificadores.
- RankPointsCalculator: suma/resta según desempeño y nivel del lobby.
- SeasonManager: temporada, reinicio parcial de rango y recompensas.
- Leaderboard: clasificación por región/servidor.
Reglas competitivas complementarias:
- Los puntos podían aumentar por victoria, buena colocación y eliminaciones.
- Las malas colocaciones/derrotas podían reducir puntos.
- Las temporadas podían aplicar reinicio parcial de rango y recompensas según rango final.
- En niveles altos adquirían mayor peso Peek & Fire, control de recoil y conocimiento de loot/spawns.

==============================================================================
22. EVENTOS Y CAMBIOS VISUALES RECUPERADOS
==============================================================================
Christmas update:
- Bandages se mostraron como Apples.
- Dressing/objetos similares tuvieron retexturización tipo Candy Cane.
Valentine's event:
- Bandages se mostraron como Roses.
- Hubo contenido/cosméticos temáticos.
Fearless Fiord update:
- Nuevo mapa.
- Nuevos vehículos.
- Nuevas armas.
- Minas.
- Ziplines.
- Gold coin arena.
- Eventos de San Valentín.
Temporadas y modos temporales referenciados:
- Pases de temporada con recompensas por niveles.
- Eventos de festividades y colaboraciones.
- Modos limitados con reglas específicas, por ejemplo restricciones de tipo de arma o enfoque en vehículos.
- Torneos comunitarios y clasificatorios regionales.

==============================================================================
22A. ECONOMÍA DEL JUEGO Y MONEDAS
==============================================================================
Gold / Oro
- Moneda gratuita obtenida al jugar.
- Utilizada en cajas o compras básicas según las versiones históricas descritas.
Diamonds / Diamantes
- Moneda premium adquirida con dinero real.
- Asociada a cosméticos, cajas premium y contenido de pago/pases según la etapa del juego.
Supply Tickets / Tickets de suministro
- Existían variantes normales, avanzadas y de élite.
- Permitían abrir cajas gacha concretas sin consumir diamantes directamente.
Shards / Fragmentos / Puzzle Pieces
- Los cosméticos duplicados podían convertirse en fragmentos.
- Los fragmentos se podían canjear por objetos cosméticos concretos.
Roses / Rosas y popularidad
- Se podían regalar rosas u objetos de popularidad a perfiles de otros jugadores.
- Aumentaban valores visibles de encanto/popularidad social.
Sistemas que se desprenden para una implementación
- Wallet / CurrencyBalance por tipo de moneda.
- RewardService para partidas, misiones y eventos.
- StoreCatalog para artículos y cajas.
- LootBox / SupplyBox con tablas de probabilidad.
- DuplicateConversion para convertir duplicados en fragmentos.
- Popularity / Charm para regalos sociales.
Monetización/progresión complementaria:
- Cajas gacha: recompensas aleatorias abiertas mediante diamantes o tickets según el tipo de caja.
- Pases de temporada: progresión con niveles/recompensas y, según la etapa, ruta gratuita y premium.
- Tienda directa: skins, paquetes y bundles.
- Microtransacciones sociales: regalos, emotes y elementos asociados al perfil.
- Recompensas de partida: oro, experiencia y ocasionalmente otros recursos/tickets según sistemas vigentes.
- Conversión de duplicados a fragmentos reduce la pérdida de valor al repetir cosméticos.
- El material complementario describe las compras principalmente como cosméticas; cualquier ventaja temporal de eventos debe tratarse por separado y verificarse según versión.

==============================================================================
22B. SISTEMA DE COSMÉTICOS (SKINS)
==============================================================================
Ropa y trajes
- Uniformes tácticos y camuflaje.
- Disfraces de pollo y dinosaurio.
- Atuendos escolares.
- Armaduras cibernéticas.
- Trajes temáticos como sirvienta y otros eventos.
Skins de armas / Weapon Finishes
- Cambiaban color y materiales.
- Algunas variantes de rareza alta modificaban también el modelo 3D.
- Algunas skins estaban asociadas a efectos o cambios visuales del Death Crate.
Paracaídas y ala delta
- Variantes con formas temáticas, dragones, naves y otros diseños.
- Algunas referencias describen estelas de humo de colores.
Vehículos
- Variantes cosméticas extravagantes para Tuk-Tuk, coches y otros vehículos.
Arquitectura sugerida para Unity
- CosmeticDefinition: id, categoría, rareza, icono, prefab/materiales.
- CharacterOutfit: cabeza, torso, piernas, calzado, accesorios.
- WeaponSkinDefinition: materiales, mesh alternativo, VFX, death-crate skin.
- ParachuteSkinDefinition / GliderSkinDefinition.
- VehicleSkinDefinition.
- CosmeticInventory y EquippedCosmetics.

==============================================================================
22C. COMUNIDAD, ESPORTS Y CONTROVERSIAS HISTÓRICAS
==============================================================================
Comunidad
- Hubo actividad importante en foros, redes sociales, YouTube y streaming.
- Creadores de contenido contribuyeron a la popularidad y difusión de estrategias/metas.
Esports / competitivo organizado
- Se realizaron torneos locales, eventos patrocinados y competiciones comunitarias/regionales.
- El sistema ranked y los eventos especiales sirvieron como base competitiva.
Controversias y problemas históricos mencionados en el material complementario
- En 2018 existió una demanda de PUBG Corp relacionada con similitudes de diseño/mecánicas.
- Hubo críticas a sistemas gacha/loot boxes.
- Se reportaron en distintos periodos problemas de bots, seguridad, sincronización y estabilidad de servidores.
Nota: esta sección aporta contexto histórico; no define mecánicas que deban replicarse en Unity.

==============================================================================
23. SISTEMAS DE JUEGO QUE SE PUEDEN DEDUCIR PARA UNITY
==============================================================================
Para una recreación fiel conviene separar los sistemas:

-- CORE --
- GameManager.
- MatchManager.
- MatchStateMachine.
- PlayerManager.
- SpawnManager.
- NetworkMatchController.

-- PLAYER --
- CharacterController.
- Movement.
- Sprint.
- Crouch.
- Jump.
- Prone si se implementa.
- Vault.
- Aim.
- First/Third person camera.
- Interaction.

-- COMBAT --
- WeaponController.
- Fire modes.
- Recoil.
- Spread.
- Reload.
- Ammo.
- Projectile/HitScan.
- Head/Body/Limb multipliers.
- Weapon swapping.
- Muzzle effects.
- Audio.

-- ATTACHMENTS --
- Muzzle slot.
- Grip slot.
- Magazine slot.
- Stock slot.
- Scope slot.

-- HEALTH --
- HP.
- Armor.
- Helmet.
- Damage zones.
- Healing.
- Temporary buffs.
- Knocked state para equipos.
- Death.

-- INVENTORY --
- Capacity.
- Backpack levels.
- Stacks.
- Loot.
- Drag/drop.
- Auto pickup.

-- WORLD --
- Loot spawner.
- Supply crate.
- Airplane.
- Parachute.
- Safe zone.
- Red/danger zones si se recrean.
- Doors.
- Buildings.
- Terrain.
- Water.
- Vehicles.
- Mines.
- Ziplines.

-- UI --
- HP.
- Armor.
- Helmet.
- Backpack.
- Ammo.
- Weapon slots.
- Crosshair.
- Scope.
- Compass.
- Minimap.
- Full map.
- Safe zone.
- Kill feed.
- Alive players.
- Team UI.
- Interaction prompts.
- Vehicle UI.
- Inventory.
- Loot window.

-- AUDIO --
- Gunshots.
- Reloads.
- Footsteps.
- Vehicles.
- Explosions.
- Zone.
- Airdrop plane.
- Parachute.
- UI.
- Directional combat indicators.

-- OPTIMIZACIÓN / RED / ESCALABILIDAD (RECOMENDACIONES, NO INTERNOS CONFIRMADOS) --
- LOD por distancia para personajes, vehículos, edificios y vegetación.
- Frustum/occlusion culling para reducir renderizado innecesario.
- Streaming de mapa/sectores para limitar memoria activa, especialmente en móvil.
- Compresión de assets y variantes de calidad por plataforma.
- Network interest management para no replicar todos los actores a todos los clientes.
- Interpolación y predicción de movimiento para jugadores/vehículos remotos.
- Compresión/priorización de paquetes y estados según relevancia.
- Telemetría para balance, rendimiento y detección de exploits.
- Validación autoritativa de impactos/acciones críticas para reducir desincronización y abuso.
- Pruebas específicas de hit registration, jitter, teleporting y pérdida de paquetes.

-- FÍSICA / BALÍSTICA (MODELO DE IMPLEMENTACIÓN SUGERIDO) --
- Colisiones diferenciadas de jugador, vehículo, mundo y proyectiles.
- Multiplicadores por cabeza/cuerpo/extremidades.
- Daño/recoil/spread configurables mediante datos, no valores hardcodeados.
- VFX de fogonazo, humo, impactos y explosiones desacoplados de la lógica de daño.

==============================================================================
24. DATOS COMPLEMENTARIOS DE ARMAS Y OBJETOS
==============================================================================
Rifles / Assault Rifles
- M4A1: descrita como equilibrada y estable, con compatibilidad amplia de accesorios.
- AKM: alto daño y recoil elevado. Aunque la referencia real es 7.62 mm, la tabla antigua del juego agrupa su consumo bajo Rifle Ammo.
- AR15: buena cadencia y recoil vertical moderado según la descripción histórica.
- M14EBR: comportamiento cercano a un DMR, útil a media/larga distancia.
- AN94: destaca por precisión en ráfagas.
- ACR: rifle estable de daño moderado y buen control.
Snipers
- AWM: descrita en una fuente como capaz de eliminar con disparo a la cabeza incluso contra casco de Nivel 3; su disponibilidad exacta es contradictoria entre fichas.
- Barrett: alto impacto y fuerte utilidad contra vehículos; asociado a supply drops.
- SVD: semiautomático en descripción general, aunque su ficha usa la etiqueta de modo Single.
- AS VAL: supresión integrada y comportamiento híbrido entre sniper y rifle automático.
- Kar98k: aparece mencionado en el bloque general como rifle de cerrojo clásico, letal y de baja cadencia, pero no existe una ficha detallada recuperada en el bloque principal.
Submachine Guns
- Thompson: cargador grande y enfoque de corto alcance.
- MP7: alta cadencia y utilidad en fases tempranas.
- Vector: cadencia muy alta, cargador pequeño y gran beneficio al usar cargador extendido.
- P90: una referencia la describe con cargador de 50 balas y fuerte desempeño en interiores.
Shotguns
- M870: bombeo, alto daño a quemarropa.
- WRO: un cartucho base, daño muy alto y alcance superior a otras escopetas según la descripción histórica.
- AA12: automática y eficaz en espacios cerrados.
- SAIGA-12: semiautomática y versátil.
LMG
- M249: cargador de 100, adecuada para fuego sostenido y daño a vehículos.
Melee
- Rubber Chicken: aumenta la velocidad de movimiento al llevarla equipada; otras fichas mencionan desvío de proyectiles.
- Frying Pan: puede funcionar como protección pasiva frente a proyectiles que impacten desde atrás según la descripción comunitaria.
- Crowbar y cuchillos: armas de proximidad con distintos perfiles de daño/velocidad.
Accesorios — efectos complementarios
- Silenciadores: reducen sonido y fogonazo; una descripción indica que disminuyen o evitan la señal de disparo en el radar visual enemigo.
- Compensadores: reducen recoil vertical y horizontal.
- Flash/Smoke Hider: reduce u oculta fogonazo y puede aportar una reducción menor de recoil.
- Triangle Grip: ayuda al recoil y a la velocidad de apuntado.
- Vertical Foregrip: reducción más directa de recoil.
- Extended Mag: aumenta capacidad.
- Quickdraw Mag: mejora recarga.
- Extended Quickdraw: combina ambos efectos.

==============================================================================
24A. BALANCE REFERENCIAL ESTIMADO POR ARMA (NO OFICIAL)
==============================================================================
IMPORTANTE:
- Este bloque proviene del compendio extendido aportado por el usuario.
- RPM, recoil cualitativo y algunas capacidades son estimaciones/recopilaciones comunitarias y pueden variar entre parches.
- No usar estos valores como configuración definitiva sin contrastar la versión histórica objetivo.

Formato: Arma | Cadencia estimada | Recoil | Notas complementarias

ASSAULT RIFLES / RIFLES
- M4A1 | ~650 RPM | Medio | 30/40; versátil y altamente configurable.
- AKM | ~600 RPM | Alto | 30/40; alto daño y control automático exigente.
- AR15 | ~700 RPM | Medio-Bajo | 30/40; buena cadencia/control.
- M14EBR | ~500 RPM en burst | Medio | Single / 3-burst; comportamiento marksman-like.
- ACR | ~650 RPM | Bajo-Medio | 30/40; mejor control que AR15 según referencia.
- AN94 | ~700 RPM en ráfaga | Medio | Single/Burst/Auto según tabla complementaria.
- AUG | ~600 RPM | Medio | 30/40; asociado a Supply Crate.

SMG
- Vector | >1200 RPM (muy alta, estimada) | Bajo | Cargador reportado de forma variable; requiere Extended Mag para explotar su cadencia.
- Thompson | ~700-800 RPM | Medio | 45/60.
- MP7 | ~900 RPM | Bajo | 30/40.
- MP5 | ~950 RPM | Bajo-Medio | 30/40.
- P90 | ~800-900 RPM | Bajo | referencia de cargador 50.
- PP19 | ~750 RPM | Medio | 25/35.

SNIPER / DMR
- AWM | ~40-50 RPM | Alto | Bolt-action; 5/10.
- Barrett | ~30-40 RPM | Muy alto | 5/10; alta eficacia contra vehículos.
- SVD | ~200-300 RPM semiauto | Medio | 10/20.
- AS VAL | ~700 RPM | Medio | 20/30; supresor integrado según ficha.
- QBU88 | ~150-250 RPM | Alto | Semiautomática; recoil mayor que SVD según material.
- M110 | ~40-60 RPM | Alto | 5/10 en tabla complementaria; clasificación/modo requieren verificación.

LMG
- M249 | ~600 RPM sostenida | Alto | cargador 100; fuerte capacidad de supresión.

SHOTGUNS
- AA12 | ~300-400 RPM | Medio | automática; capacidad exacta presenta inconsistencias.
- M870 | ~60-80 RPM | Alto | 5; pump-action.
- WRO | ~20-30 RPM | Muy alto | 1 disparo base.
- SAIGA-12 | ~200-300 RPM | Medio | capacidad reportada 5-10 en tabla complementaria.
- M1887 | ~60-120 RPM | Alto | 2; modo exacto varía según ficha.

PISTOLS
- Desert Eagle | ~60-80 RPM | Alto | 7; ficha sin accesorios.
- G18C | ~800-900 RPM | Bajo | cargador 17-19 en tabla complementaria; ficha original incompleta.

Interpretación recomendada para Unity:
- Guardar estos valores en ScriptableObjects/DataAssets editables.
- Mantener un campo DataConfidence (Verified / Community / Estimated / Contradictory).
- Separar FireRateRPM, RecoilVertical, RecoilHorizontal, SpreadHip, SpreadADS y DamageProfile para poder ajustar cada parche sin modificar código.

==============================================================================
24B. CONSEJOS TÁCTICOS POR ROL (REFERENCIA COMUNITARIA)
==============================================================================
Francotirador
- Priorizar altura y cobertura.
- AWM/Barrett para alto impacto; SVD/QBU88 para seguimiento a media/larga distancia.
- Cambiar de posición tras disparar para reducir exposición.
Asalto
- M4A1/AR15/ACR como opciones versátiles.
- Compensador + grip para mejorar control según compatibilidad.
- Red Dot/2x para CQB y 4x para media distancia según preferencia.
Apoyo / supresión
- M249 para mantener presión y bloquear rotaciones.
- Gestionar munición y posición para sostener fuego continuo.
Reconocimiento / movilidad
- Vehículos ligeros para rotaciones rápidas o discretas según situación.
- Smoke y stun como herramientas de entrada, extracción y rescate.
Nota: son consejos derivados de meta/comunidad, no reglas del sistema.

==============================================================================
25. INCONSISTENCIAS Y PUNTOS QUE DEBEN VERIFICARSE
==============================================================================
1. Tamaño de Ghillie Island
- Una parte del material la describe como 4 x 4 km.
- Otra indica aproximadamente 4.8 x 4.8 km.
- Recomendación: verificar escala con un mapa histórico o vídeo antes de construir el terreno definitivo.
2. AWM — disponibilidad
- Una descripción la asocia fuertemente a Airdrops/Supply Crates.
- La ficha detallada recuperada indica "Everywhere".
- No fijar su tabla de spawn definitiva sin evidencia adicional.
3. M14EBR — disponibilidad
- La propia información comunitaria se contradice entre Supply Crate y aparición normal por el mapa.
4. M14EBR — clasificación
- Se etiqueta como Assault Rifle, aunque por comportamiento se describe como marksman/DMR.
5. M249 — clasificación
- Oficialmente Light Machine Gun.
- Algunas tablas antiguas de la wiki la agrupan con Assault Rifles.
6. SVD — modo de fuego
- La ficha pone Single.
- La descripción general la trata como semiautomática.

==============================================================================
7. M110
==============================================================================
- El material lo describe como bolt-action.
- Existen errores tipográficos históricos en la wiki (M1110) y conviene contrastar animación y cadencia con material visual.
8. Curación: Bandage / Med Kit / First Aid Kit
- Un bloque dice Bandage +10 hasta 75%, First Aid hasta 75% y Medkit a 100%.
- El bloque de Supplies indica Bandage +10, Med Kit +50 y First Aid Kit restaurando HP completo.
- Este es uno de los grupos de datos que más necesita verificación por versión.
9. Sports Drink y Cardio Tonic
- El material expresa sus efectos como +HP y boost de velocidad.
- En otra descripción general los boosters se interpretan como una barra de energía que regenera gradualmente.
- Se recomienda modelarlos inicialmente como "boost/energy" configurable, no como curación fija irreversible en código.

==============================================================================
10. AA12
==============================================================================
- Existen resultados indexados que no coinciden completamente sobre capacidad/compatibilidad.
11. Rifle Muzzle en Fandom
- Se indica explícitamente que algunas filas estaban vandalizadas.
- No usar esas descripciones textuales como fuente de balance.
12. Rankings de armas
- Hay diferencias entre la ficha individual de un arma y rankings comunitarios posteriores.
- Deben tratarse como meta/opinión de una época, no como dato oficial de rendimiento.
13. Munición
- Las categorías Rifle Ammo, SMG Ammo, Pistol Ammo, SG Ammo y SR Ammo son la abstracción utilizada por la tabla antigua.
- No equivalen necesariamente a calibres reales.
- La tabla parece anterior a varias armas de Fearless Fiord, por lo que no cubre todo el arsenal posterior.

14. Cadencia (RPM) y recoil del compendio extendido
- Son estimaciones comunitarias, no cifras oficiales verificadas por versión.
- Deben mantenerse desacopladas de los valores definitivos de balance.
15. Vector / G18C / SAIGA-12 — capacidades complementarias
- El compendio extendido aporta rangos/capacidades que no aparecen completos en la documentación principal.
- Conservarlos como referencia, no como dato definitivo.
16. Internos técnicos (LOD, predicción, compresión de red)
- Son recomendaciones plausibles para una recreación masiva en Unity y aparecen en el compendio complementario.
- No deben presentarse como arquitectura interna confirmada del Rules of Survival original sin evidencia adicional.

==============================================================================
26. CHECKLIST DE FIDELIDAD PARA UNA RECREACIÓN EN UNITY
==============================================================================

-- PLAYER / MOVIMIENTO --
- Caminar, correr y sprint.
- Saltar.
- Agacharse.
- Prone si se confirma en la versión objetivo.
- Vault / superar obstáculos.
- Lean izquierda/derecha.
- Movimiento durante ADS.
- Cámara tercera persona y, si corresponde, primera persona.
- Interacción contextual.
- Caída, aterrizaje y paracaídas.

-- COMBATE --
- Hitscan/proyectiles según arma.
- Single, Auto y Burst.
- Recoil configurable por arma.
- Spread por postura/movimiento/ADS.
- Recarga y recarga rápida.
- Daño a cabeza, cuerpo y extremidades.
- Daño a vehículos.
- Supresión/fogonazo/sonido.
- Cambio de arma y slots.
- Melee.
- Granadas, humo, stun, molotov y RPG.

-- INVENTARIO --
- Capacidad máxima.
- Coste de capacidad por objeto.
- Mochila L1/L2/L3.
- Capacidad adicional por body armor cuando corresponda.
- Stacks.
- Auto-pickup.
- Drop y split stack.
- Loot de mundo y Death Crate.
- Equipar/desequipar accesorios automáticamente o manualmente.

-- MUNDO / PARTIDA --
- Lobby/pre-match.
- Avión y ruta de vuelo.
- Salto y paracaídas.
- Loot spawners.
- Airdrops.
- Safe Zone por fases.
- Daño fuera de zona.
- Edificios, puertas, terreno y agua.
- Minas y ziplines en Fearless Fiord.
- Vehículos y combustible.
- Condición de victoria.

-- HUD / UI --
- Minimap.
- Mapa completo.
- Brújula.
- Radar visual de sonido.
- Vida, armadura, casco y mochila.
- Munición y armas.
- Crosshair y scopes.
- Jugadores vivos.
- Kill feed.
- Panel de equipo.
- Interacciones.
- Loot window.
- Inventario.
- UI de zona segura.
- UI de vehículo.
- HUD táctil personalizable.

-- AUDIO --
- Pasos por superficie.
- Disparos por arma.
- Recargas.
- Impactos y daño.
- Explosiones.
- Vehículos.
- Avión de airdrop.
- Paracaídas.
- Zona segura.
- UI.
- Audio direccional compatible con los indicadores del HUD.

-- DATOS / SCRIPTABLEOBJECTS RECOMENDADOS --
- WeaponDefinition.
- AmmoDefinition.
- AttachmentDefinition.
- ArmorDefinition.
- BackpackDefinition.
- ConsumableDefinition.
- ThrowableDefinition.
- VehicleDefinition.
- LootTableDefinition.
- MapDefinition.
- SafeZonePhaseDefinition.
- RankDefinition.
- CurrencyDefinition.
- CosmeticDefinition.

-- PRIORIDAD DE VALIDACIÓN VISUAL --
1. HUD y distribución de controles.
2. Escala y POIs de mapas.
3. Modelos y proporciones de armas.
4. Animaciones de movimiento/ADS/recarga.
5. Valores reales de daño, recoil y cadencia.
6. Spawn de armas y loot.
7. Curación/boosters.
8. Vehículos y físicas.
9. Sonidos e indicadores direccionales.
10. Cosméticos y eventos.
