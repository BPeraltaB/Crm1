# Instrucciones del proyecto CRM1

## 1. Proyecto, arquitectura y tecnología

CRM1 es una aplicación de escritorio para realizar un seguimiento exhaustivo de los clientes actuales y de los futuros clientes desde el primer contacto.

El proyecto se organizará en capas y utilizará formularios Windows Forms desarrollados en C#.

Los datos se guardarán inicialmente en archivos locales. Se podrán utilizar los formatos JSON, TXT y binario según lo definido en las especificaciones. SQL podrá incorporarse en el futuro cuando se defina en una especificación.

## 2. Comandos

- Compilar el proyecto desde Visual Studio.
- Ejecutar la aplicación para comprobar que funciona.
- Si hay errores, corregirlos y volver a compilar.

## 3. Estilo

- Entorno de desarrollo: Visual Studio 2022.
- El idioma del código será inglés.
- Los comentarios del código serán en español.

## 4. Reglas

- Antes de modificar código, leer `docs/constitution.md` y la especificación activa en `specs/`. Si alguno de esos documentos no existe o no está claro cuál es la especificación activa, solicitar aclaración antes de tocar el código.
- No añadir dependencias ni cambiar los formatos de almacenamiento (JSON, TXT, binario o SQL) sin actualizar primero la especificación correspondiente.
- No modificar archivos de ningún tipo salvo que el usuario lo solicite explícitamente.

## 5. Memoria

- Leer `memory.md` para conocer el estado del proyecto y las decisiones tomadas en sesiones anteriores.
- Al terminar una tarea, actualizar `memory.md` con la información relevante para continuar el proyecto.
- Registrar las decisiones importantes y los errores que se deban evitar.
- Eliminar de `memory.md` la información que haya dejado de ser útil.
- Si una información de `memory.md` debe convertirse en una regla permanente, proponer su incorporación a `agents.md` en lugar de mantenerla como memoria de sesión.

## 6. Al terminar cualquier tarea

Compilar y confirmar en la respuesta si la compilación y las pruebas pasan.
