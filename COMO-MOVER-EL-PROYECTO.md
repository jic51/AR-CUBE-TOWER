# Cómo sacar el proyecto de OneDrive

Guía paso a paso. No hace falta saber programar.

---

## Por qué hay que moverlo

El proyecto vive hoy en `C:\Users\remod\OneDrive\Documents\JoseCastro\APPS\AR-CUBE-TOWER`, es decir, dentro de una carpeta que OneDrive sincroniza con la nube.

Unity y OneDrive chocan. Mientras trabajas, Unity reescribe sin parar miles de archivos en la carpeta `Library/`, y OneDrive intenta subir cada uno de esos cambios a internet. El resultado son bloqueos de archivo a media compilación, importaciones que se corrompen y builds lentos.

**OneDrive no te está protegiendo.** El respaldo de verdad ya lo tienes en GitHub, que guarda cada versión del proyecto con su historial. OneDrive solo estorba.

---

## Por qué copiar la carpeta a mano no funciona

Si intentas copiar y pegar la carpeta del proyecto, Windows casi siempre se atasca. Siempre por lo mismo: la carpeta `Library/` contiene decenas de miles de archivos pequeños que Unity está usando en ese momento.

Lo importante es esto: **`Library/` no hay que copiarla nunca.** Unity la reconstruye sola en unos minutos. No contiene nada tuyo, es caché.

Por eso el método correcto no es copiar, es **descargar una copia limpia desde GitHub**.

---

## Antes de empezar: rescata el video

Casi todo tu proyecto está respaldado en GitHub. **Una sola cosa no lo está:** el video de desarrollo, porque pesaba 399 MB y GitHub no acepta archivos de más de 100 MB.

Si lo quieres conservar, ve a la carpeta vieja, entra en `Assets\Videos\`, y copia el archivo `.mp4` a cualquier sitio seguro (el Escritorio sirve).

Si no lo necesitas, sáltate este paso.

---

## Los pasos

### Paso 1 — Cierra Unity

La ventana del editor y también Unity Hub.

### Paso 2 — Abre PowerShell

Pulsa la tecla Windows, escribe `powershell` y ábrelo. Da igual en qué carpeta se abra.

### Paso 3 — Crea la carpeta nueva

Escribe esta línea y pulsa Enter:

```
mkdir C:\Dev
```

Si responde que la carpeta ya existe, perfecto, sigue adelante.

### Paso 4 — Entra en ella

```
cd C:\Dev
```

### Paso 5 — Descarga el proyecto

```
git clone https://github.com/jic51/AR-CUBE-TOWER.git
```

Tarda un par de minutos. Verás líneas de progreso con porcentajes.

Cuando termine, tendrás el proyecto completo y actualizado en `C:\Dev\AR-CUBE-TOWER`.

**Esta copia ya trae el cambio de identificador.** No hace falta `git pull` ni `git stash`: viene limpia de fábrica.

### Paso 6 — Ábrelo en Unity

1. Abre **Unity Hub**
2. Pulsa el botón **Add** (en algunas versiones, **Open**)
3. Elige **Add project from disk**
4. Selecciona la carpeta `C:\Dev\AR-CUBE-TOWER`
5. Haz clic sobre el proyecto para abrirlo

**La primera vez tarda varios minutos** y la pantalla puede parecer congelada. Es normal: Unity está reconstruyendo la carpeta `Library/` desde cero. Déjalo trabajar y no lo cierres.

### Paso 7 — Comprueba que todo está bien

Cuando Unity termine de abrir:

1. **Edit → Project Settings**
2. En la lista de la izquierda: **Player**
3. Pestaña del **robot de Android**
4. Despliega **Other Settings**
5. Busca **Identification → Package Name**

Debe decir:

```
io.github.jic51.arcubetower
```

Si dice eso, la mudanza salió bien.

Abre también la escena del juego (`Assets/Scenes/ARTowerGame.unity`) y comprueba que se ve como esperas.

---

## Solo cuando todo funcione: la carpeta vieja

**No borres nada hasta haber completado el paso 7 con éxito.**

Cuando la copia nueva funcione, la vieja de OneDrive ya no sirve para nada y conviene eliminarla: si se queda ahí, es fácil abrir la equivocada por error y volver a trabajar sobre la carpeta mala sin darte cuenta.

Bórrala desde el Explorador de Windows, con Unity cerrado.

### Si Windows no deja borrarla

Aparece una ventana **«Folder In Use — the folder or a file in it is open in another program»**, y debajo, **«Availability status: Sync pending»**.

**No es un problema de permisos**, aunque lo parezca. Ser administrador no permite borrar un archivo que otro programa tiene abierto en ese momento. Y quien lo tiene abierto es OneDrive, que está sincronizando la carpeta.

**Cierra OneDrive:**

1. Abajo a la derecha, en la barra de tareas, busca el ícono de **nube** (puede estar oculto tras la flechita `^`)
2. Haz clic en él
3. Arriba a la derecha del panel, clic en el **engranaje**
4. Elige **Quit OneDrive** / **Cerrar OneDrive** y confirma

Comprueba también que Unity esté cerrado y que no haya ninguna ventana del Explorador abierta dentro de esa carpeta. Luego inténtalo de nuevo.

**Si sigue sin dejarte:** reinicia la computadora y borra la carpeta antes de abrir cualquier otra cosa. Al arrancar todavía nada la tiene tomada.

### Si no quieres pelearte con esto ahora

Borrarla es limpieza, no un requisito: no bloquea nada. La alternativa de un segundo es **renombrarla** a `AR-CUBE-TOWER-VIEJO-NO-USAR`. Así ya no la abres por error —que era el único riesgo real— y la borras otro día.

---

## Cómo evitar que vuelva a pasar

A partir de ahora, **todos los proyectos de programación van en `C:\Dev\`**, nunca dentro de OneDrive, Dropbox, Google Drive ni Documentos sincronizados.

Tu respaldo es GitHub, y es mejor que cualquiera de esos: guarda el historial completo, puedes volver a cualquier versión anterior, y se abre desde cualquier computadora del mundo.

La rutina de respaldo sigue siendo la de siempre: al terminar de trabajar, `git add`, `git commit` y `git push`.

---

## Si algo sale mal

Nada de lo que hagas en tu computadora puede dañar lo que está en GitHub. Si la copia nueva se rompe, bórrala y repite desde el paso 3: vuelve a descargarse entera.

Es la ventaja de tener el proyecto en un repositorio — la carpeta de tu disco es desechable.
