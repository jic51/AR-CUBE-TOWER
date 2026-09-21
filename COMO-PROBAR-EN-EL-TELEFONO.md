# Cómo probar el juego en el teléfono

Guía paso a paso. No hace falta saber programar ni usar la consola.

**Carpeta del proyecto en esta computadora:**
`C:\Users\remod\OneDrive\Documents\JoseCastro\APPS\AR-CUBE-TOWER`

---

## Por qué falló el intento del 2026-09-21

El juego **sí se compiló bien**. Lo único que falló fue copiarlo al teléfono.

El teléfono tiene instalada una versión vieja del juego. Android se negó a reemplazarla porque la versión nueva viene "firmada" con una llave distinta a la vieja. Android hace esto a propósito: es lo que impide que un desconocido publique una actualización falsa de una app que no es suya.

No se puede convencer a Android de lo contrario. Pero sí se puede rodear el problema, y de paso aplicamos un cambio que ya estaba pendiente.

---

## La solución: 5 pasos

Al hacer esto, el juego pasa a tener un nombre interno nuevo (`io.github.jic51.arcubetower` en vez de `com.unity.jc.ARTowerGame`). Para el teléfono será **una aplicación distinta**, así que se instalará sin pelearse con la vieja. La vieja se queda ahí, sin molestar, y se puede borrar cuando se quiera.

### Paso 1 — Cerrar Unity del todo

Cierra la ventana del editor de Unity **y también Unity Hub**.

Esto no es opcional. Unity guarda la configuración del proyecto en su memoria y la vuelve a escribir en el disco al cerrarse. Si está abierto mientras llega el cambio del paso 2, Unity lo borra sin avisar. **Esto fue lo que pasó la vez anterior.**

### Paso 2 — Traer el cambio desde GitHub

Elige la forma que ya uses:

**Si usas GitHub Desktop:**

1. Abre GitHub Desktop
2. Arriba, asegúrate de que el repositorio seleccionado sea `AR-CUBE-TOWER`
3. Pulsa el botón **Fetch origin**
4. Ese mismo botón cambiará a **Pull origin** — púlsalo

**Si prefieres escribir el comando:**

1. Abre el Explorador de archivos en `C:\Users\remod\OneDrive\Documents\JoseCastro\APPS\AR-CUBE-TOWER`
2. Haz clic derecho sobre un espacio vacío dentro de la carpeta
3. Elige **Abrir en Terminal** (en Windows 11) o **Abrir ventana de PowerShell aquí** (en Windows 10, manteniendo Shift al hacer clic derecho)
4. Escribe esto y pulsa Enter:

```
git pull
```

Debe aparecer un texto que mencione `ProjectSettings.asset`. Si en cambio sale un mensaje de error o algo sobre *conflict*, **detente ahí y manda ese texto** — no sigas.

### Paso 3 — Abrir Unity y comprobar que el cambio llegó

1. Abre Unity y carga el proyecto
2. En el menú de arriba: **Edit → Project Settings**
3. En la lista de la izquierda, elige **Player**
4. Busca la fila de pestañas con iconos y pulsa el del **robot de Android**
5. Despliega la sección **Other Settings**
6. Dentro, busca el apartado **Identification** y la casilla **Package Name**

**Debe decir exactamente:**

```
io.github.jic51.arcubetower
```

Si sigue diciendo `com.unity.jc.ARTowerGame`, el cambio no llegó. En ese caso escríbelo a mano en esa casilla y pulsa Enter — funciona igual.

### Paso 4 — Compilar e instalar

1. Conecta el teléfono a la computadora por cable USB
2. Desbloquea la pantalla del teléfono y déjala encendida
3. En Unity: **File → Build Profiles**

   *(En Unity 6 se llama así. En versiones anteriores era "Build Settings".)*

4. Comprueba que en la lista de plataformas esté seleccionada **Android**
5. Pulsa el botón **Build And Run**
6. Te pedirá dónde guardar el archivo. Ponle un nombre nuevo, por ejemplo `AR-CUBE-TOWER-v2`, para no confundirlo con el anterior

Ahora hay que esperar. La primera compilación tardó 20 minutos; esta debería ser algo más rápida.

### Paso 5 — Comprobar

Cuando termine, **el juego arranca solo en el teléfono**. No hay que buscarlo ni abrirlo a mano.

Si en lugar de eso sale una ventana de error en Unity: copia el texto completo y mándalo.

---

## Qué probar una vez que arranque

Estos cuatro arreglos se escribieron pero **nunca se han probado en un teléfono de verdad**. Son la razón por la que este build importa:

| Qué probar | Qué debería pasar |
|---|---|
| Ver un anuncio **estando acostado o mirando a una pared** | El anuncio aparece igual a los 8 segundos. El juego **no** se queda congelado |
| Perder una partida a propósito, dos veces seguidas | **No** regalan monedas gratis por perder |
| Gastar las 5 vidas y tratar de jugar otra vez | Bloquea la entrada, avisa "No lives left!" y abre la tienda |
| Ver varios anuncios seguidos el mismo día | La recompensa baja: 50, luego 35, luego 20, luego 10 monedas |

Si alguno no se comporta así, anótalo y lo revisamos.

---

## Cosas que NO hay que hacer ahora

- **No** hace falta desinstalar la app vieja. Con el nombre interno nuevo ya no estorba
- **No** uses la consola ni `adb` si no te resulta cómodo. Los 5 pasos de arriba no lo necesitan

---

## Si el teléfono no aparece

Si Unity dice que no encuentra ningún dispositivo:

1. En el teléfono: **Ajustes → Acerca del teléfono**
2. Toca **7 veces seguidas** sobre **Número de compilación**. Aparecerá un aviso de que ya eres desarrollador
3. Vuelve a **Ajustes → Sistema → Opciones de desarrollador**
4. Activa **Depuración por USB**
5. Desconecta y vuelve a conectar el cable. En el teléfono saldrá una ventana preguntando si autorizas a esta computadora — acepta y marca la casilla de recordar
