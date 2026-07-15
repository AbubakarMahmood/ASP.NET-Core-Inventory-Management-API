# Deployment

## Single-container demo (`huggingface/`)

The `huggingface/` folder packages the API and PostgreSQL into one
self-contained container image — useful for demo hosts that give you a
single container and nothing else. It was written as a Hugging Face Docker
Space definition (Docker Spaces now require a paid HF plan) but works on
any host that builds a Dockerfile: point the platform at these three files,
expose port 7860, and the API serves Swagger as its landing page.

Data lives inside the container, so it resets to the seeded demo set on
every restart — intended for demos, not real use.

## Full stack

`Dockerfile` + `docker-compose.yml` at the repository root run the real
topology (API, PostgreSQL, Blazor UI) on any Docker host:

```bash
docker compose up --build
```

For anything beyond a local demo, supply real values for
`JwtSettings__SecretKey` and the connection string via environment
variables, and restrict `Cors__AllowedOrigins` to your UI's origin.
