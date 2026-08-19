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
