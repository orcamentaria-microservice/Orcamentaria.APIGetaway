# Orcamentaria • API Getaway (.NET 8)

Gateway leve para **roteamento** de requisições entre microsserviços do ecossistema Orcamentaria. Integra-se ao **Service Registry** para **descoberta de serviços**, aplica regras de roteamento e pode publicar mensagens de erro em **RabbitMQ**, valida e faz o balance das instancias dos serviços.

---

## 🧱 Stack & Arquitetura
- **.NET 8** (ASP.NET Core Web API)
- Integração com **Service Registry**
- Mensageria: **RabbitMQ** (filas de erro)
- Versionamento de API: `api/v1/...`

Estrutura do repositório:
```
Orcamentaria.APIGetaway/
 ├── Orcamentaria.APIGetaway.API/
 ├── Orcamentaria.APIGetaway.Application/
 ├── Orcamentaria.APIGetaway.Domain/
 ├── Orcamentaria.APIGetaway.Infrastructure/
 └── Orcamentaria.APIGetaway.sln
```

---

## ⚙️ Configuração (appsettings.json)

- **MessageBrokerConfiguration** :
  - `BrokerName`, `Host`, `Port`, `UserName`, `Password`
  - `ErrorQueue`, `ErrorCriticalQueue`
- **ServiceRegistryConfiguration**:
  - `BrokerName`
---

- `ServiceRegistryConfiguration`: aponta para o endpoint do Service Registry (baseUrl, rotas de descoberta).
- `MessageBrokerConfiguration`: host/port/credenciais do RabbitMQ e filas de erro.
- Use `appsettings.Development.json` para overrides locais. **Nunca** commitar segredos reais.

---

## ▶️ Executando localmente

```bash
dotnet restore
dotnet build
dotnet run --project Orcamentaria.APIGetaway.API
```

---

## 🧭 Endpoints (v1)

### RoutingController
- **POST** `/api/v1/Routing` — encaminha a requisição conforme as regras do Gateway (descoberta via Service Registry).

Payload de exemplo (RequestDTO) esperado pelo roteador deve conter informações como **serviceName**, **endpointName**, **httpMethod**, **pathParams**/**query**/**body** (ver implementação do `RoutingService`).

---

## 🔐 Considerações de segurança
- O Gateway **não deve** aceitar tokens de usuário para rotas internas de configuração; use **token de serviço** quando necessário.
- Valide **método/rota** antes de encaminhar para evitar _open proxy_.
- **Timeouts e circuit breakers** são recomendados (Polly/HttpClientFactory).

---

## 📈 Observabilidade
- Logue correlação (`X-Correlation-Id`) e resposta dos _downstreams_.
- Em caso de erro, publique em `error`/`error_critical` (RabbitMQ) com contexto da chamada.

---

## ✨ Autor

**Marcelo Fernando**  
Desenvolvedor Fullstack | Arquitetura de Microserviços