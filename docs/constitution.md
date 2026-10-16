# Constitución del proyecto CRM1
1. **Stack simple:** C# y .NET WinForms; añadir dependencias solo si resuelven una necesidad concreta.
2. **Spec sincronizada:** toda función documentada debe estar implementada o marcada como pendiente; los cambios actualizan la documentación.
3. **Responsabilidades separadas:** los formularios presentan la interfaz; las reglas de negocio y el acceso a datos viven fuera de ellos.
4. **Tests obligatorios:** las reglas de negocio y la persistencia nuevas o modificadas deben tener pruebas automatizadas.
5. **Datos fiables:** guardar localmente; un fallo de lectura o escritura nunca debe presentarse como una operación exitosa ni descartar datos silenciosamente.
6. **Idioma coherente:** los nombres del código (clases, métodos, variables) estarán en inglés; los comentarios del código, la interfaz y los mensajes al usuario estarán en español.
