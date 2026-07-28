# Deployment policy

The supported repository deployment is the root Docker Compose topology:
separate API, PostgreSQL, and static-web containers. Copy `.env.example` to
`.env`, replace both secrets, and run `docker compose up --build`.

The former single-container Hugging Face definition was removed. It cloned a
moving public branch during the image build, ran PostgreSQL beside the API,
embedded a signing key, and used ephemeral storage. Those properties made it
an unsuitable security or reproducibility example.

For a real environment, apply migrations as a release step rather than from
application startup, disable demo seeding and OpenAPI, terminate TLS at a
trusted proxy, back up PostgreSQL, and store the JWT/database secrets in the
platform secret manager. See [`../docs/OPERATIONS.md`](../docs/OPERATIONS.md).
