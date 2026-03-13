# 🔍 GUÍA PARA VER LOGS Y DIAGNOSTICAR ERRORES DE LOGIN

## 📍 **DÓNDE VER LOS LOGS**

### **1. CONSOLA DE VISUAL STUDIO (PRINCIPAL)** ⭐

Esta es la **forma más fácil y completa** de ver todos los logs:

#### **Pasos:**
1. En Visual Studio, ve a: **Ver → Salida** (o presiona `Ctrl + Alt + O`)
2. En el dropdown superior, selecciona: **"Depurar"** o **"FabricaHilos - ASP.NET Core Web Server"**
3. Ejecuta la aplicación (F5 o Ctrl+F5)
4. Intenta hacer login
5. **VERÁS TODOS LOS LOGS AQUÍ** 👇

#### **Ejemplo de lo que verás:**

```
═══════════════════════════════════════════════════
🔐 INICIO DE PROCESO DE LOGIN
═══════════════════════════════════════════════════
👤 Usuario ingresado: JPEREZ
🔑 Contraseña ingresada: ******
🔄 Paso 1: Validando contra Oracle Database...

═══════════════════════════════════════════
🔍 INTENTANDO CONECTAR A ORACLE DATABASE
═══════════════════════════════════════════
👤 Usuario recibido: JPEREZ
🔑 Contraseña recibida: ******
🔌 Cadena de conexión: Data Source=10.0.7.11:1521/ORCL;User Id=...
📝 Query SQL: select c_user, psw_sig, c_costo from cs_user where c_user = :puser and psw_sig = :ppsw
🔄 Abriendo conexión a Oracle...

❌ ERROR DE ORACLE: ORA-12170: TNS:Connect timeout occurred
   Código de error: 12170
   
ó

✅ Conexión a Oracle abierta exitosamente
🔍 Ejecutando query y leyendo resultados...
✅ USUARIO ENCONTRADO EN ORACLE
   - c_user: JPEREZ
   - c_costo: VENTAS
```

---

### **2. TERMINAL/CONSOLA (Alternativa)**

Si ejecutas desde terminal con `dotnet run`:

```powershell
cd D:\.Net\FabricaHilos
dotnet run --project FabricaHilos/FabricaHilos.csproj
```

Los logs aparecerán en la misma ventana del terminal.

---

### **3. NAVEGADOR - CONSOLA DEL NAVEGADOR**

1. Abre la aplicación en el navegador
2. Presiona **F12**
3. Ve a la pestaña **"Console"** / **"Consola"**
4. Si el login es exitoso desde Oracle, verás:

```javascript
═══════════════════════════════════════════════════
🔐 DATOS DE LOGIN - ORACLE DATABASE
═══════════════════════════════════════════════════
👤 Usuario Oracle: JPEREZ
🏢 Centro de Costo: VENTAS
```

---

### **4. NAVEGADOR - PESTAÑA NETWORK (Para ver errores HTTP)**

1. Presiona **F12** → Ve a **"Network"** / **"Red"**
2. Haz login
3. Busca la petición **POST** a `/Account/Login`
4. Haz clic en ella
5. Ve a **"Response"** o **"Respuesta"**
   - Si hay error, verás el HTML con el mensaje

---

## 🐛 **ERRORES COMUNES Y CÓMO IDENTIFICARLOS**

### **Error 1: No se puede conectar a Oracle**

#### **Síntomas en los logs:**
```
❌ ERROR DE ORACLE: ORA-12170: TNS:Connect timeout occurred
   Código de error: 12170
```

#### **Causas posibles:**
- Oracle no está accesible desde tu red
- IP `10.0.7.11` no es alcanzable
- Firewall bloqueando puerto 1521
- Oracle Database apagado

#### **Soluciones:**
```powershell
# 1. Verificar conectividad
ping 10.0.7.11

# 2. Verificar puerto Oracle
Test-NetConnection -ComputerName 10.0.7.11 -Port 1521

# 3. Verificar cadena de conexión en appsettings.json
```

---

### **Error 2: Usuario no encontrado en Oracle**

#### **Síntomas en los logs:**
```
❌ USUARIO NO ENCONTRADO EN ORACLE
   El usuario 'JPEREZ' no existe o la contraseña es incorrecta
```

#### **Causas posibles:**
- El usuario no existe en la tabla `cs_user`
- La contraseña es incorrecta
- El campo `c_user` en Oracle tiene espacios o mayúsculas/minúsculas

#### **Soluciones:**
```sql
-- Verificar en Oracle si el usuario existe:
SELECT c_user, psw_sig, c_costo 
FROM cs_user 
WHERE c_user = 'JPEREZ';

-- Ver todos los usuarios:
SELECT * FROM cs_user;
```

---

### **Error 3: No se puede crear usuario en Identity**

#### **Síntomas en los logs:**
```
❌ No se pudo crear usuario en Identity para JPEREZ
   Errores: Passwords must have at least one non alphanumeric character.
```

#### **Causas posibles:**
- La contraseña no cumple con las políticas de Identity
- Requisitos: mayúscula, minúscula, dígito, 6+ caracteres

#### **Soluciones:**
Modificar `Program.cs` para hacer menos estricta la política:

```csharp
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;  // ← Cambiar a false
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 4;  // ← Reducir a 4
    options.Password.RequireNonAlphanumeric = false;
})
```

---

### **Error 4: Cadena de conexión incorrecta**

#### **Síntomas en los logs:**
```
❌ ERROR GENERAL AL CONECTAR CON ORACLE
   Tipo: ArgumentException
   Mensaje: Invalid connection string
```

#### **Solución:**
Verificar `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "OracleConnection": "Data Source=10.0.7.11:1521/ORCL;User Id=SIG;Password=STARK;"
  }
}
```

---

## 📝 **CHECKLIST DE DIAGNÓSTICO**

Cuando tengas problemas de login, verifica en orden:

### **1. Verifica la Consola de Visual Studio** ✅
- [ ] ¿Aparece "🔐 INICIO DE PROCESO DE LOGIN"?
- [ ] ¿Aparece "🔍 INTENTANDO CONECTAR A ORACLE DATABASE"?
- [ ] ¿Hay algún error rojo?

### **2. Verifica Conectividad Oracle** ✅
```powershell
ping 10.0.7.11
Test-NetConnection -ComputerName 10.0.7.11 -Port 1521
```

### **3. Verifica Credenciales** ✅
- [ ] ¿El usuario existe en la tabla `cs_user`?
- [ ] ¿La contraseña es correcta?
- [ ] ¿Hay espacios extras en usuario o contraseña?

### **4. Verifica appsettings.json** ✅
```json
"OracleConnection": "Data Source=10.0.7.11:1521/ORCL;User Id=SIG;Password=STARK;"
```

---

## 🚀 **PASOS PARA DIAGNOSTICAR TU PROBLEMA ACTUAL**

### **Paso 1: Ejecuta la aplicación en modo Debug**

1. En Visual Studio, presiona **F5** (con debugging)
2. Ve a **Ver → Salida** (`Ctrl + Alt + O`)
3. Selecciona **"Depurar"** en el dropdown

### **Paso 2: Intenta hacer login**

1. Ingresa tu usuario y contraseña
2. Presiona "Ingresar"

### **Paso 3: Lee los logs en la consola**

Busca estas líneas clave:

```
═══════════════════════════════════════════════════
🔐 INICIO DE PROCESO DE LOGIN
═══════════════════════════════════════════════════
```

**Si ves esto:**
```
✅ Conexión a Oracle abierta exitosamente
✅ USUARIO ENCONTRADO EN ORACLE
```
→ **Oracle funciona correctamente**

**Si ves esto:**
```
❌ ERROR DE ORACLE: ORA-XXXXX
```
→ **Hay problema con Oracle** (copia el error completo)

**Si ves esto:**
```
❌ USUARIO NO ENCONTRADO EN ORACLE
```
→ **El usuario no existe o la contraseña es incorrecta**

### **Paso 4: Comparte los logs**

Copia TODO el texto desde:
```
═══════════════════════════════════════════════════
🔐 INICIO DE PROCESO DE LOGIN
```

Hasta:
```
═══════════════════════════════════════════════════
```

Y compártelo para poder ayudarte mejor.

---

## 📞 **NECESITAS MÁS AYUDA**

Si después de revisar los logs no puedes identificar el problema:

1. **Copia los logs completos** de la Ventana de Salida
2. **Toma captura** de pantalla de cualquier error
3. **Indica**:
   - ¿El usuario existe en Oracle?
   - ¿Puedes conectarte a Oracle desde otra herramienta (SQL Developer, DBeaver)?
   - ¿Qué usuario estás intentando usar?

---

## ✅ **PRUEBA RÁPIDA**

Ejecuta esto para probar la conexión a Oracle:

```powershell
# 1. Verificar ping
ping 10.0.7.11

# 2. Verificar puerto
Test-NetConnection -ComputerName 10.0.7.11 -Port 1521

# 3. Ejecutar aplicación con logs detallados
cd D:\.Net\FabricaHilos
dotnet run --project FabricaHilos/FabricaHilos.csproj --verbosity detailed
```

¡Con estos logs detallados podrás identificar exactamente dónde está el problema! 🎯
