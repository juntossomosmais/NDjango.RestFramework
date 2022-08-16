# csharp-rest-framework

O [csharp-rest-framework](https://github.com/juntossomosmais/csharp-rest-framework), ou `AspNetCore.RestFramework.Core`, é um framework para ASP.NET desenvolvido pela Juntos Somos Mais para tornar mais prática e uniforme a criação de APIs REST.

## Sumário

- [csharp-rest-framework](#csharp-rest-framework)
  - [Sumário](#sumário)
  - [Ordenação](#ordenação)
  - [Filtros](#filtros)
    - [QueryStringFilter](#querystringfilter)
    - [QueryStringIdRangeFilter](#querystringidrangefilter)
    - [QueryStringSearchFilter](#querystringsearchfilter)
    - [Implementando um filtro](#implementando-um-filtro)
  - [Erros](#erros)
  - [Validação](#validação)
  - [Serializer](#serializer)
  - [Exemplo de uso](#exemplo-de-uso)
    - [Entidade](#entidade)
    - [Entity Framework](#entity-framework)
    - [DTO](#dto)
    - [Validação](#validação-1)
    - [Include de entidades filhas/pais](#include-de-entidades-filhaspais)
    - [Controller](#controller)
  - [Dicionário](#dicionário)

## Ordenação

No método `ListPaged`, usamos os _query parameters_ `sort` ou `sortDesc` para fazer a ordenação por um campo. Se não for informado, usaremos sempre o campo `Id` da entidade para ordenação crescente.

## Filtros

Filtros são mecanismos aplicados sempre que tentamos obter os dados de uma entidade nos métodos `GetSingle` e `ListPaged`.

### QueryStringFilter

O `QueryStringFilter`, talvez o mais relevante, é um filtro que faz _match_ dos campos passados nos _query parameters_ com os campos da entidade cujo filtro é permitido. Todos os filtros são criados usando como operador o _equals_ (`==`).

### QueryStringIdRangeFilter

O `QueryStringIdRangeFilter` é um filtro cujo objetivo principal é atender o método `getMany` do React Admin, um framework utilizado em um dos nossos produtos, o Portal Admin (novo). Seu objetivo é, caso o parâmetro `ids` seja passado, filtrar as entidades pelo `Id` baseado em todos os `ids` informados nos _query parameters_.

### QueryStringSearchFilter

O `QueryStringSearchFilter` é um filtro que permite que seja informado um parâmetro `search` nos _query parameters_ para fazer uma busca, através de um único input, em vários campos da entidade, inclusive fazendo `LIKE` em strings.

### Implementando um filtro

Dado um `IQueryable<T>` e um `HttpRequest`, você pode implementar o filtro da maneira como preferir. Basta herdar da classe base e adicionar ao seu _controller_:

```cs
public class MyFilter : AspNetCore.RestFramework.Core.Filters.Filter<Seller>
{
    private readonly string _forbiddenName;

    public MyFilter(string forbiddenName)
    {
        _forbiddenName = forbiddenName;
    }

    public IQueryable<TEntity> AddFilter(IQueryable<TEntity> query, HttpRequest request)
    {
        return query.Where(m => m.Name != forbiddenName);
    }
}
```

```cs
public class SellerController
{
    public SellerController(...)
        : base(...)
    {
        Filters.Add(new MyFilter("Exemplo"));
    }
}
```

## Erros

Utilizamos como padrão de erros [este aqui](https://votorantimindustrial.sharepoint.com/:w:/r/sites/juntos_somos_mais/_layouts/15/Doc.aspx?sourcedoc=%7BE8895835-9397-4E19-9046-26D7168A931A%7D&file=Padr%C3%A3o%20de%20retorno%20das%20APIs.docx&action=default&mobileredirect=true), que é o padrão indicado no nosso [playbook de C#](https://github.com/juntossomosmais/playbook/blob/master/backend/csharp.md).

No framework, as classes `ValidationErrors` e `UnexpectedError` já implementam esse contrato, sendo retornadas no `BaseController` em caso de erros de validação ou outra exception.

## Validação

Para validação, também conforme o playbook, devemos utilizar **FluentValidation**. Basta implementar os _validators_ dos DTOs e configurar sua aplicação com a extensão (2) `ModelStateValidationExtensions.ConfigureValidationResponseFormat` para garantir que, caso o `ModelState` seja inválido, um `ValidationErrors` seja retornado com um `400 Not Found`.

Além disso
- (1) Devemos usar o `JSM.FluentValidations.AspNet.AsyncFilter`;
- (3) **Talvez** se faça necessário acessar o `HttpContext` nos _validators_, para isso basta adicionar o acessor aos serviços.

```cs
services.AddControllers()
    ...
    // no final do AddControllers, adicione o seguinte:
    // 1 - JSM.FluentValidations.AspNet.AsyncFilter
    .AddModelValidationAsyncActionFilter(options =>
    {
        options.OnlyApiController = true;
    })
    // 2 - ModelStateValidationExtensions
    .ConfigureValidationResponseFormat();

// 3 - acessor do HttpContext
services.AddHttpContextAccessor();
```

## Serializer

O `Serializer` é um mecanismo utilizado pelo `BaseController` como se fosse um _service_. Nele, estão as lógicas utilizadas pelo _controller_, e este _serializer_ é injetado em cada um. Ele pode ter seus métodos sobrescritos, por exemplo, para lógicas adicionais ou diferentes em entidades específicas.

## Exemplo de uso

Vamos, como exemplo, criar uma API para uma suposta entidade `Customer`.

### Entidade

Algumas características das entidades:

- Devemos herdar de `BaseModel<TPrimaryKey>`.
  - O `TPrimaryKey` é o tipo da chave primária, no marketplace costumamos usar `Guid`.
- O `GetFields` é um método cuja implementação é obrigatória. Ele informa quais campos de uma entidade serão serializados na _response_ da API.
  - Para campos de entidades filhas ou pais, podemos usar o `:` para indicá-las para que também sejam serializadas. Nesse caso, é necessário fazer o `Include` com um filtro.

```cs
public class Customer : BaseModel<Guid>
{
    public string Name { get; set; }
    public string CNPJ { get; set; }
    public int Age { get; set; }

    public ICollection<CustomerDocument> CustomerDocument { get; set; }

    public override string[] GetFields()
    {
        return new[] { "Id", "Name", "CNPJ", "Age", "CustomerDocument", "CustomerDocument:DocumentType", "CustomerDocument:Document" };
    }
}
```

### Entity Framework

Basta adicionar a collection ao `DbContext` da aplicação:

```cs
public class ApplicationDbContext : DbContext
{
    public DbSet<Customer> Customer { get; set; }
}
```

### DTO

No DTO, precisamos apenas herdar de `BaseDto<TPrimaryKey>`, parecido com a entidade, e colocar lá.

```cs
public class CustomerDto : BaseDto<Guid>
{
    public CustomerDto()
    {
    }

    public string Name { get; set; }
    public string CNPJ { get; set; }
    
    public ICollection<CustomerDocumentDto> CustomerDocuments { get; set; }
}
```

### Validação

Com o DTO criado, se necessário, devemos criar o _validator_ para ele. Abaixo, temos um exemplo bem simples dessa implementação:

```cs
public class CustomerDtoValidator : AbstractValidator<CustomerDto>
{
    public CustomerDtoValidator(IHttpContextAccessor context)
    {
        RuleFor(m => m.Name)
            .MinimumLength(3)
            .WithMessage("Name should have at least 3 characters");

        if (context.HttpContext.Request.Method == HttpMethods.Post)
            RuleFor(m => m.CNPJ)
                .NotEqual("567")
                .WithMessage("CNPJ cannot be 567");
    }
}
```


### Include de entidades filhas/pais

Precisamos criar um filtro para dar o `Include` na entidade filha `CustomerDocument`. Simples assim:

```cs
public class CustomerDocumentIncludeFilter : Filter<Customer>
{
    public override IQueryable<Customer> AddFilter(IQueryable<Customer> query, HttpRequest request)
    {
        return query.Include(x => x.CustomerDocument);
    }
}
```

### Controller

Agora, basta criar o _controller_ da entidade para criar a API.

Perceba que o `AllowedFields` é usado para configurar a maioria dos filtros de _query string_.

```cs
[Route("api/[controller]")]
[ApiController]
public class CustomersController : BaseController<CustomerDto, Customer, Guid, ApplicationDbContext>
{
    public CustomersController(
        CustomerSerializer serializer,
        ApplicationDbContext dbContext,
        ILogger<Customer> logger)
        : base(
                serializer,
                dbContext,
                logger)
    {
        AllowedFields = new[] {
            nameof(Customer.Id),
            nameof(Customer.Name),
            nameof(Customer.CNPJ),
            nameof(Customer.Age),
        };

        Filters.Add(new QueryStringFilter<Customer>(AllowedFields));
        Filters.Add(new QueryStringSearchFilter<Customer>(AllowedFields));
        Filters.Add(new QueryStringIdRangeFilter<Customer, Guid>());
        Filters.Add(new CustomerDocumentIncludeFilter());
    }
}
```

**Importante!** Perceba que o o nome está no plural, isso se deve às convenções padrões do React Admin.

## Dicionário

Para facilitar o entendimento de alguns termos e tipos, eis um dicionário com suas descrições:

| Termo | Descrição
|---|---|
| `TPrimaryKey` | Tipo da chave primária de uma entidade, geralmente é `Guid`.
| `TEntity` | Tipo da entidade da qual estamos falando numa classe genérica.
| `TOrigin` | No `BaseController`, é o mesmo que o `TEntity`.
| `TDestination` | Tipo do DTO.
| `TContext` | Tipo do contexto do Entity Framework.