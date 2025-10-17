# Eventos de Integração - Resumo das Alterações

## 📋 O que foi implementado?

Foi criado um mecanismo para separar **Eventos de Domínio** (processados internamente) de **Eventos de Integração** (publicados em message brokers para comunicação entre microserviços).

## 🔧 Arquivos Criados

### 1. **IIntegrationEventPublisher.cs**
Interface para publicação de eventos de integração em brokers de mensagens.

### 2. **DefaultIntegrationEventPublisher.cs**
Implementação padrão que apenas loga os eventos. Deve ser substituída por uma implementação real.

### 3. **Documentação e Exemplos**
- `docs/IntegrationEventsPublisher.md` - Documentação completa
- `examples/IntegrationEventExamples.cs` - Exemplos de eventos
- `examples/MessageBrokerImplementations.cs` - Implementações para diferentes brokers

## 🔄 Arquivos Modificados

### 1. **Mediator.cs**
- Adicionada constante `IntegrationEventsQueueKey`
- Diferenciação entre `IDomainEvent` e `IIntegrationEvent`
- Eventos de integração são enfileirados separadamente para publicação no broker

### 2. **Middleware.cs**
- Processa eventos de domínio internamente (MediatR)
- Publica eventos de integração no broker via `IIntegrationEventPublisher`
- Mantém transação garantindo que eventos só são publicados após commit

### 3. **ServiceCollectionExtensions.cs**
- Registra `IIntegrationEventPublisher` com implementação padrão
- Usa `TryAddScoped` permitindo que seja substituído facilmente

## 🚀 Como Usar

### 1. Criar um Evento de Integração

```csharp
public record OrderCreatedIntegrationEvent(
    Guid OrderId,
    Guid CustomerId,
    decimal TotalAmount,
    DateTime CreatedAt
) : IIntegrationEvent;
```

### 2. Publicar o Evento

```csharp
await _mediator.Publish(new OrderCreatedIntegrationEvent(
    order.Id,
    order.CustomerId,
    order.TotalAmount,
    DateTime.UtcNow
));
```

### 3. Configurar o Broker (exemplo com RabbitMQ/MassTransit)

```csharp
// Program.cs
services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });
    });
});

services.AddSharedInfra<YourDbContext>();

// Substituir implementação padrão
services.AddScoped<IIntegrationEventPublisher, RabbitMqIntegrationEventPublisher>();
```

## 📦 Brokers Suportados (com exemplos)

- ✅ RabbitMQ (via MassTransit)
- ✅ Azure Service Bus
- ✅ AWS SQS
- ✅ Apache Kafka
- ✅ Google Cloud Pub/Sub
- ✅ In-Memory (para testes)

Todos os exemplos estão em `examples/MessageBrokerImplementations.cs`

## 🔍 Fluxo de Execução

1. **Request recebido** → Middleware inicia transação
2. **Handler executa** → Publica eventos via `_mediator.Publish()`
3. **Mediator** → Enfileira eventos (separando domínio vs integração)
4. **Response enviado** → Middleware processa filas:
   - Eventos de domínio → Processados por handlers internos (MediatR)
   - Eventos de integração → Publicados no message broker
5. **Commit da transação** → Só após todos eventos processados

## ⚠️ Importante

- Por padrão, a implementação apenas **loga** os eventos de integração
- Você **DEVE** implementar seu próprio `IIntegrationEventPublisher` para usar um broker real
- Use `services.AddScoped<IIntegrationEventPublisher, SuaImplementacao>()` após `AddSharedInfra()`

## 🎯 Diferenças Entre Eventos

| Característica | IDomainEvent | IIntegrationEvent |
|----------------|--------------|-------------------|
| **Escopo** | Interno ao microserviço | Entre microserviços |
| **Processamento** | MediatR handlers | Message Broker |
| **Quando usar** | Regras de negócio internas | Comunicação entre bounded contexts |
| **Exemplo** | `OrderDomainEventCreated` | `OrderCreatedIntegrationEvent` |

## 📚 Próximos Passos

1. Escolha seu message broker (RabbitMQ, Azure Service Bus, etc.)
2. Instale o pacote NuGet correspondente
3. Implemente `IIntegrationEventPublisher`
4. Registre sua implementação no DI
5. Crie seus eventos de integração
6. Publique-os usando `_mediator.Publish()`

Para mais detalhes, consulte `docs/IntegrationEventsPublisher.md` e os exemplos em `examples/`.
