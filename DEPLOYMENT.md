# Despliegue — CI/CD con GitHub Actions

Este documento describe cómo quedan configurados el pipeline de build/publish a GHCR y el
despliegue automático en el servidor privado, y qué pasos manuales (una sola vez) hacen falta
para que funcione.

Los workflows viven en `.github/workflows/deploy-backend.yml` y
`.github/workflows/deploy-frontend.yml`. Cada uno tiene dos jobs: `build` (en un runner de GitHub,
compila y publica la imagen en GHCR) y `deploy` (en el runner self-hosted del servidor, la baja y
reinicia el contenedor correspondiente). Un push a `develop` que solo toca `Club12-Backend/**`
dispara únicamente el workflow de backend, y viceversa; un cambio en `docker-compose.yml` o en los
workflows dispara ambos.

## 1. Prerrequisitos del servidor

- Host Debian con Docker Engine + Docker Compose v2 instalados (`docker compose version` debe
  responder `v2.x`).
- El usuario que corre el runner (`ghrunner`) debe pertenecer al grupo `docker`:

  ```bash
  sudo usermod -aG docker ghrunner
  # cerrar sesión y volver a entrar (o `newgrp docker`) para que el cambio de grupo tome efecto
  ```

  Verificar como ese usuario, sin `sudo`:

  ```bash
  docker ps
  ```

  Si falla por permisos, el grupo no se aplicó todavía.

## 2. Registrar los runners self-hosted (manual, una vez por runner)

Se necesitan **dos runners registrados por separado**, uno por cada `runs-on` label. Un mismo
runner no puede sostener dos jobs en simultáneo.

Obtener un token de registro en:
`https://github.com/FrancoRu/Taller-de-Integracion/settings/actions/runners/new`

Backend, en `/home/ghrunner/actions-runner` (o el directorio equivalente):

```bash
./config.sh --url https://github.com/FrancoRu/Taller-de-Integracion \
            --token <REGISTRATION_TOKEN> \
            --name club12-back \
            --labels Club-12-back-runner \
            --work _work
sudo ./svc.sh install ghrunner && sudo ./svc.sh start
```

Repetir en un **segundo directorio de runner, separado**, con:

```bash
./config.sh --url https://github.com/FrancoRu/Taller-de-Integracion \
            --token <REGISTRATION_TOKEN> \
            --name club12-front \
            --labels Club-12-front-runner \
            --work _work
sudo ./svc.sh install ghrunner && sudo ./svc.sh start
```

Notas importantes:

- `self-hosted` se agrega automáticamente al registrar el runner — **no** hay que pasarlo en
  `--labels`.
- Los strings de label son sensibles a mayúsculas/minúsculas y deben coincidir exactamente con los
  workflows: `Club-12-back-runner` y `Club-12-front-runner`.
- Ambos runners corren en el mismo host (`192.168.0.200`), sobre el mismo proyecto de compose
  (`club12`) — por diseño, para que ambos jobs de `deploy` puedan compartir el mismo directorio de
  compose sin pisarse.

## 3. Bootstrap del `.env` de producción (manual, una sola vez)

El pipeline **nunca** crea, lee ni sobrescribe el `.env` de producción. Si falta, el paso de
sincronización del job `deploy` falla de forma explícita y detiene el workflow.

```bash
mkdir -p /home/docker-compose/Club12
cp .env.example /home/docker-compose/Club12/.env
# completar cada valor CHANGE_ME en /home/docker-compose/Club12/.env
chmod 600 /home/docker-compose/Club12/.env
```

Este archivo vive de forma permanente en `/home/docker-compose/Club12/`, un directorio distinto al
workspace efímero que usa `actions/checkout` — por eso `git clean -ffdx` del checkout nunca lo
toca.

## 4. Visibilidad del paquete en GHCR

Los paquetes de GHCR son **privados por defecto**, incluso en un repositorio público. El job de
`deploy` ya hace `docker login ghcr.io` con un token con permiso `read:packages`, así que el
`pull` funciona sin cambios adicionales. Alternativa: si se prefiere evitar el login, se puede
cambiar la visibilidad del paquete a público desde la UI de GHCR
(`Package settings → Change visibility`).

## 5. Rollback manual

Si un deploy deja el servicio en mal estado, se puede volver a la imagen anterior directamente en
el servidor, sin pasar por GitHub:

```bash
cd /home/docker-compose/Club12
docker tag ghcr.io/francoru/club12-backend:previous ghcr.io/francoru/club12-backend:latest
docker compose up -d --no-deps --no-build backend
```

(Reemplazar `backend` por `frontend` y el nombre de imagen correspondiente para el frontend.)

## 6. Desarrollo local (sin cambios)

`docker-compose.yml` conserva los bloques `build:` como fallback local — el pipeline de CI nunca
los usa (`--no-build` en cada deploy), pero siguen funcionando para levantar el proyecto en la
máquina de desarrollo:

```bash
docker compose build && docker compose up -d
```

En un host que no sea Linux, la ruta absoluta del bind mount del servicio `db`
(`/home/docker/club12/db`) no aplica: usar un `docker-compose.override.yml` (ignorado por
git) que reemplace esa línea de volumen por un volumen nombrado local.

## 7. Migración a Postgres self-hosted (manual, una sola vez)

A partir del cambio `selfhosted-postgres-db`, `docker-compose.yml` incluye un servicio
`db` (`postgres:17-alpine`) en la red interna `club12`, **sin puerto publicado**. El
backend deja de hablar con Supabase Postgres y pasa a usar ese contenedor. Supabase
**Storage** (buckets de imágenes y fichas médicas) se sigue usando igual — no se toca.

El pipeline de CI **no** levanta `db` (`deploy-backend.yml` usa `--no-deps`). El servicio
se levanta una vez en el cutover y se mantiene solo por `restart: unless-stopped`.

### 7.1 Preparar el host

```bash
# Directorio de datos de Postgres — en /home (420 GB), NO en la partición raíz (31 GB)
sudo mkdir -p /home/docker/club12/db
sudo chown 999:999 /home/docker/club12/db      # uid del usuario postgres en la imagen alpine

# Directorio de backups
sudo mkdir -p /home/docker/backups/club12
```

### 7.2 Completar el `.env` de producción

En `/home/docker-compose/Club12/.env` (owner `gh-runner:gh-runner`, mode 600) agregar las
tres claves nuevas y reescribir la connection string. Elegir la contraseña ahora
(`openssl rand -base64 24`); `Username`/`Password` de la connection string **deben
coincidir** con `POSTGRES_USER`/`POSTGRES_PASSWORD`.

```bash
POSTGRES_USER=postgres
POSTGRES_DB=postgres
POSTGRES_PASSWORD=<contraseña-fuerte-elegida-ahora>

# reemplaza el valor anterior (pooler de Supabase):
ConnectionStrings__DbConnection=Host=db;Port=5432;Database=postgres;Username=postgres;Password=<misma-contraseña>;SSL Mode=Disable

# activa el backup periódico + restore-desde-panel ya incluido en la app
# (reemplaza al cron manual — ver §7.5):
Backup__Enabled=true
```

> Las variables `POSTGRES_*` sólo inicializan la base en el **primer arranque** del
> contenedor (directorio de datos vacío). Después, para cambiar la contraseña hay que
> hacerlo con `ALTER USER` dentro de la base.

### 7.3 Dump fresco de Supabase

Desde el host, usando `postgres:17-alpine` como cliente (no depende de la versión de
`pg_dump` del host). Los datos del app viven en **dos** schemas: `public` (Identity /
`AspNet*`) y `Club12` (todo lo demás). Pasá las credenciales como variables de entorno
para no pelear con el URL-encoding de la contraseña; sacá los valores del `.env` actual
(`grep -i DbConnection /home/docker-compose/Club12/.env`) — `User Id=` → `PGUSER`,
`Password=` → `PGPASSWORD`, `Server=` → `PGHOST`.

```bash
set +H                          # bash: desactiva la expansión de '!' en la password
cd /home/docker/backups/club12

docker run --rm -v "$PWD:/out" \
  -e PGHOST=aws-1-us-east-2.pooler.supabase.com -e PGPORT=5432 -e PGDATABASE=postgres \
  -e PGUSER='<User Id>' -e PGPASSWORD='<Password>' -e PGSSLMODE=require \
  postgres:17-alpine \
  pg_dump --format=custom --no-owner --no-privileges \
  --schema=public --schema='"Club12"' \
  --file=/out/club12-cutover-$(date +%F).dump
```

> `--schema='"Club12"'` **con comillas dobles adentro**: `pg_dump` pasa los patrones de
> `--schema` a minúsculas, así que `--schema=Club12` no matchea nada y el dump sale con
> solo la mitad de las tablas.

Verificá que estén los dos schemas:

```bash
docker run --rm -v "$PWD:/out" postgres:17-alpine \
  pg_restore --list "/out/club12-cutover-$(date +%F).dump" | grep "TABLE DATA"
```

Tenés que ver ~22 tablas en `Club12` (`Teams`, `Players`, `Tournaments`, `Matches`, …) y
8 en `public` (`AspNetUsers`, `__EFMigrationsHistory`, …).

### 7.4 Cutover (ventana corta — el backend se reinicia)

```bash
cd /home/docker-compose/Club12
DUMP=/home/docker/backups/club12/club12-cutover-$(date +%F).dump

# 1. Levantar SOLO la base y esperar a que esté healthy
docker compose up -d db
docker compose ps db                       # STATUS = healthy

# 2. Restaurar el dump dentro del contenedor
docker compose cp "$DUMP" db:/tmp/restore.dump
docker compose exec -T db pg_restore --no-owner --no-privileges -U postgres -d postgres /tmp/restore.dump
docker compose exec -T db rm /tmp/restore.dump
#    El único error esperado es `schema "public" already exists` (1, ignorado) — benigno.
#    Cualquier otro error rojo: frená y revisá antes de seguir.

# 3. Verificación
docker compose exec -T db psql -U postgres -d postgres -c '\dt "Club12".*'
docker compose exec -T db psql -U postgres -d postgres -c 'select count(*) from "Club12"."Teams";'
docker compose exec -T db psql -U postgres -d postgres -c 'select count(*) from "public"."AspNetUsers";'

# 4. Con el .env ya editado (7.2 — cambiar ConnectionStrings__DbConnection a Host=db;…),
#    recrear el backend
docker compose up -d backend
docker compose logs --tail=40 backend      # sin errores de migración, "Now listening on: http://[::]:8080"

# 5. Bouncear el frontend: su nginx tiene cacheada la IP vieja del backend → si no,
#    todo /api/* da 502 hasta reiniciarlo.
docker compose restart frontend

# 6. Verificar (el backend no está publicado en el host — va por el proxy :5001)
docker compose ps
curl -s -o /dev/null -w 'http=%{http_code}\n' 'http://localhost:5001/api/tournaments?pageSize=300'
```

En el arranque, `MigrateAsync` ve el schema ya restaurado y no hace nada (o aplica sólo
migraciones más nuevas que el dump). El seed está en `Seed:Enabled=false` en producción.

> El paso 5 (`restart frontend`) queda automatizado en `deploy-backend.yml` para los
> deploys de CI; en el cutover manual hay que hacerlo a mano.

### 7.5 Backup automático (feature integrado del backend)

El backend ya trae un sistema de backups completo — no hace falta cron ni script en el
host. Se activa con la variable agregada en §7.2:

```bash
Backup__Enabled=true
# opcionales, si se quiere otra cadencia (defaults entre paréntesis):
# Backup__IntervalHours=24      (24)
# Backup__RetentionCount=7      (7)
```

Con `Backup__Enabled=true`, un `DatabaseBackupHostedService` corre dentro del propio
proceso backend cada `Backup__IntervalHours` horas, genera el dump con `pg_dump` (ya
instalado en la imagen del backend) y lo guarda en el volumen `backup-data`
(`Backup__LocalStoragePath`, default `/app/backups`), podando al `Backup__RetentionCount`
más reciente.

- **Ver los backups:** panel admin → sección de backups. Cada fila muestra su origen
  ("Programado" para los automáticos, "Manual" para los que se disparan a mano desde el
  mismo panel).
- **Restaurar:** botón de restore en el panel (`POST /api/backups/{id}/restore`) — sin
  SSH al servidor. Antes de restaurar, la app crea automáticamente un backup de
  seguridad del estado actual.
- **Verificar la integridad de un dump sin arriesgar producción:** descargarlo desde el
  panel y restaurarlo en una base "scratch" separada (no usar el botón de restore de
  producción solo para testear que un dump sirve).

### 7.6 Rollback

```bash
# En el .env, volver ConnectionStrings__DbConnection al valor del pooler de Supabase
docker compose up -d backend
```

No hay migración de schema que revertir. Dejar el contenedor `db` y
`/home/docker/club12/db` en su lugar hasta confirmar que el cutover quedó estable; recién
ahí dar de baja el proyecto de Supabase (los buckets de Storage siguen en uso).

### 7.7 `docker compose down` — cuidado

Los datos de `db` son un bind mount: sobreviven `down` y `down -v`. Aun así, `-v`
elimina los volúmenes nombrados (`backup-data`), y borrar `/home/docker/club12/db` a mano
destruye la base.

## Checklist post-merge

Estos pasos **no están cubiertos por el cambio de código en sí** — son responsabilidad del
usuario/operador una vez que este cambio se mergea a `develop`, y deben verificarse a mano porque
ningún runner self-hosted existe todavía en este momento:

- [ ] Registrar ambos runners con las labels exactas `Club-12-back-runner` y
  `Club-12-front-runner` (sección 2).
- [ ] Crear el `.env` real en `/home/docker-compose/Club12/.env` a partir de `.env.example`
  (sección 3).
- [ ] Hacer un push chico a `develop` que solo toque `Club12-Backend/**` y confirmar que:
  - solo corre `deploy-backend.yml` (no `deploy-frontend.yml`);
  - el job `build` publica `ghcr.io/francoru/club12-backend:latest`;
  - el job `deploy` corre después de que `build` termina, en el runner `Club-12-back-runner`;
  - `docker images` muestra `club12-backend:previous` (o el mensaje de "primer deploy" si es la
    primera vez);
  - `docker compose ps backend` muestra el contenedor recreado y saludable;
  - el contenedor de `frontend` **no** se reinicia.
- [ ] Repetir el mismo push de prueba tocando solo `Club12-WebClient/**` y confirmar el mismo
  comportamiento en espejo para `deploy-frontend.yml`.
- [ ] Confirmar que `/home/docker-compose/Club12/.env` sigue existiendo después del deploy y que
  el servicio toma sus valores (por ejemplo, que la API arranca sin errores de configuración).
- [ ] Confirmar que `docker image prune -f` no deja crecer una pila de imágenes colgantes, y que
  las tags `:latest` y `:previous` sobreviven.
