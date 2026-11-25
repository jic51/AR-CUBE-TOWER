# Cómo crear la keystore de Android

Guía paso a paso. No hace falta la consola: Unity lo hace todo.

**Tiempo:** unos 15 minutos, casi todos en decidir dónde guardarla.

---

## Qué vas a crear

Un archivo con la clave que firma tu app. Android compara la firma de cada actualización con la de la versión instalada: si no coinciden, rechaza la instalación. Es lo que impide que un desconocido publique una actualización falsa de tu juego.

Dos consecuencias que conviene tener claras antes de empezar:

- **Google Play no acepta la keystore de pruebas.** Sin una propia no se puede publicar.
- **Si pierdes el archivo o su contraseña después de publicar, no puedes actualizar la app nunca más.** No hay recuperación ni soporte: Google no guarda copia. La app queda congelada y la única salida es publicar otra desde cero, con ficha nueva y cero descargas.

Por eso la mitad de esta guía trata de dónde guardarla.

---

## Antes de empezar: decide dónde va

**No la guardes dentro de la carpeta del proyecto.** Dos motivos: si se borra la carpeta, se va con ella — estuvo a punto de pasar en septiembre con OneDrive —, y es fácil que acabe subida a algún sitio sin querer.

Crea una carpeta aparte, solo para esto. Por ejemplo:

- En Mac: `~/Documentos/ARCubeTower-Firma/`
- En Windows: `C:\ARCubeTower-Firma\`

El repositorio ya está configurado para ignorar archivos `.keystore` y `.jks` por si alguno acaba dentro del proyecto, pero esa red de seguridad no sustituye a guardarla fuera.

---

## Los pasos en Unity

### Paso 1 — Abre la configuración de firma

1. Abre el proyecto en Unity
2. **Edit → Project Settings**
3. En la lista de la izquierda: **Player**
4. Pestaña del **robot de Android**
5. Despliega la sección **Publishing Settings**

### Paso 2 — Crea la keystore

1. Marca la casilla **Custom Keystore**
2. Pulsa el desplegable **Keystore...** y elige **Create New → Anywhere...**
3. Navega hasta la carpeta que creaste y ponle un nombre, por ejemplo `arcubetower.keystore`
4. Guarda

Unity te pedirá una contraseña y su confirmación. **Esa contraseña es tan importante como el archivo:** sin ella, la keystore no sirve para nada.

### Paso 3 — Crea la clave (el "alias")

Dentro de la misma keystore hay que crear una clave. Unity te muestra un formulario:

| Campo | Qué poner |
|---|---|
| **Alias** | `arcubetower` |
| **Password** / **Confirm** | Puede ser la misma que la del keystore. Apunta cuál usaste |
| **Validity (years)** | **50** (el valor por defecto). Google exige que siga siendo válida mucho después de 2033 |
| **First and Last Name** | Tu nombre |
| **Organizational Unit** | Déjalo vacío |
| **Organization** | `jic51`, o el nombre que vayas a usar |
| **City / State / Country Code** | Tu ciudad, Utah, `US` |

Pulsa **Add Key**.

Los campos de nombre y organización son informativos: quedan dentro del certificado, pero **no los verá nadie en Play**. No te detengas en ellos.

### Paso 4 — Comprueba que quedó activa

De vuelta en **Publishing Settings**, debe aparecer:

- **Custom Keystore** marcado
- La ruta de tu archivo en **Keystore**
- `arcubetower` en **Alias**

Si Unity te pide la contraseña al compilar, es normal: la pide una vez por sesión y **no la guarda en el proyecto**.

---

## Lo más importante: el respaldo

Hazlo hoy, no "cuando publique".

**Respalda tres cosas juntas:**

1. El archivo `.keystore`
2. La contraseña del keystore
3. La contraseña del alias

**Dónde.** Un gestor de contraseñas que permita adjuntar archivos (1Password, Bitwarden) es la mejor opción: guarda el archivo y las claves en el mismo sitio, cifrado. Un disco externo también sirve. Lo que no vale es dejarlo en un solo lugar.

Aquí sí tiene sentido un respaldo en la nube, a diferencia del proyecto: **el riesgo de perderla es mucho mayor que el de filtrarla**, y va protegida por contraseña. Pero que sea una cuenta privada, y que la contraseña sea larga.

**Una cosa que mucha gente descubre tarde:** perder la contraseña es tan irreversible como perder el archivo. No la memorices, guárdala.

---

## Si trabajas en dos computadoras

Unity guarda en `ProjectSettings.asset` **la ruta** de la keystore, y ese archivo sí va a Git. Si la ruta es algo como `/Users/jose/Documentos/...`, en la otra computadora no existirá y Unity dará error al compilar.

No es grave y no hay que arreglarlo en Git: copia la keystore a la segunda máquina y vuelve a seleccionarla allí, en **Publishing Settings → Keystore...**, apuntando a su copia local.

---

## Una comprobación antes de hacer commit

Unity no debería escribir las contraseñas en `ProjectSettings.asset`, y hoy no lo hace. Pero conviene mirarlo la primera vez, porque ese archivo sí se sube al repositorio.

Después de configurar la keystore, antes de hacer commit, busca en `ProjectSettings/ProjectSettings.asset` las palabras `AndroidKeystorePass` y `AndroidKeyaliasPass`.

- Si **no aparecen**, o aparecen vacías: todo bien.
- Si aparecen **con tu contraseña escrita**, bórrala del archivo antes de hacer commit y avísame.

Lo que sí va a aparecer, y es correcto, son `AndroidKeystoreName` (la ruta) y `AndroidKeyaliasName` (`arcubetower`). Eso no es secreto.

---

## Cuando llegue el momento de publicar

En Play Console te ofrecerán activar **Play App Signing**. Actívalo.

Con eso Google guarda la clave real de la app y tú conservas solo una *upload key* para subir versiones. Si algún día pierdes la upload key, Google te deja generar otra — la red de seguridad que de otro modo no existe.

Dos matices: se configura al publicar, no antes, y no te libra de crear la keystore ahora, porque la necesitas para firmar lo que subas.
