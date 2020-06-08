[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=vip32_Zeta&metric=alert_status)](https://sonarcloud.io/dashboard?id=vip32_Zeta)

# Zeta

A mildly opiniated modern cloud service architecture blueprint + reference implementation

### Layers & Dependencies

``` 

                                                                    - Services, Jobs, Validators
                                                                    - Commands/Query + Handlers
                                                .----------------.  - Messages/Queues + Handlers
   - WebApi/Mvc/                            .-->| Application    |  - Adapter Interfaces, Exceptions
     SPA/Console program host              /    `----------------`  - View Models + Mappings
                                          /        |        ^
  .--------------.                       /         |        |
  .              |     .--------------. /          V        |  - Events, Aggregates, Services
  | Presentation |     |              |/        .--------.  |  - Entities, ValueObjects
  | .Web|Tool    |---->| Presentation |-------->| Domain |  |  - Repository interfaces
  |  Service|*   |     |              |\        `--------`  |  - Specifications, Rules
  |              |     `--------------` \          ^        |                                         
  `--------------`                       \         |        |                                    
                       - Composition Root \        |        |     
                       - Controllers       \    .----------------.  - Interface Implementierungen (Adapters/Repositories)  
                       - Razor Pages        `-->| Infrastructure |  - DbContext
                       - Hosted Services        `----------------`  - Data Entities + Mappings   

```

### Service Integration
TODO: describe service discovery/registration

```
    Service A                                  Service B
  .-----------------.                             .------------------.
  | .-------------. |                             | .-------------.  |
  | | Application |-|---------------------------->|>| Application |  |
  | "-------------" |                             | "-------------"  |
  |                 |         - Messaging         |                  |
  |  /""""""""\     |         - Queueing          |  /""""""""\      |
  | / Domain   \    |         - HTTP requests     | / Domain   \     |
  | \  Model   /    |         - gRPC              | \  Model   /     |
  |  \--------/     |                             |  \--------/      |
  |                 |                             |                  |
  | .-----------.   |                             | .-----------.    |
  | | Storage   |   |                             | | Storage   |    |
  | "-----------"   |                             | "-----------"    |
  "-----------------"                             "------------------"

```

# Foundation
### Building Blocks:
#### Extension methods
- Safe()
- ...
#### Mapping
- IMapper<TSource,TDestination>
#### Utilities
- Factory

# Presentation Layer

### Building Blocks:
#### Controllers [TODO]
#### ViewModel [TODO] + mapping
#### CompositionRoot [TODO]


# Application Layer
This layer is responsible for orchestration: implements high-level logic which manipulates domain objects and starts domain workflows.
It does not contain any first-class business logic or state itself, but organizes that logic or state via calls to/from the Domain layer.
The Application layer performs persistence operations using the injected persistence interfaces.
Here the Domain Repository pattern comes into play.
This layer should pass ViewModels back to the Presentation layer (Application.Web), not Domain Entities.
Mapping takes care of this.

### Building Blocks:

```
                                                        . -mediator.Send()
                                                       /
 .------------.     .------------.     .------------. /   .------------------.
 | ASP.Net    |---->| Controller |---->| Command    |---->| Command          |
 |            |     |  -route    |     | /Query     |     | /Query Handler   |
 `------------`     `------------`     `------------`     `------------------`
```

#### Commands [TODO]
#### Queries [TODO]
#### Services [TODO]
#### Jobs
[Quartz](https://www.quartz-scheduler.net/) based jobs are used in the services.
Jobs should trigger a Command which is then being handled by a CommandHandler. The CommandHandler can use alle usual dependencies from the Application or Domain layer.
When a job starts is determined by the configured cron expression.

```
                                                        . -mediator.Send()
                                                       /
 .------------.     .------------.     .------------. /   .------------.
 | Quartz     |---->| Job        |---->| Command    |---->| Command    |
 |  Scheduler |     |  -cron     |     |            |     |  Handler   |
 `------------`     `------------`     `------------`     `------------`
```

Jobs are registered like this (CompositionRoot):

```
services.AddJobScheduling(); // register Quartz 
services.AddScopedJob<EchoJob>("0/5 * * * * ?"); // every 5 seconds
```

# Domain Layer
This layer is built out using [Domain Driven Design](https://en.wikipedia.org/wiki/Domain-driven_design) principles, nothing in it has any knowledge of anything outside it (Application or Infrastructure).

### Building Blocks:
### Entity
Should not only contain properties, otherwise it cannot express Domain concepts.
The Domain model consists of one or more Entities. All Entities should be marked with the IEntity interface

[Evans] *"Many objects are not fundamentally defined by their attributes, but rather by a thread of continuity and identity."*

#### ValueObject
[TODO]
https://martinfowler.com/bliki/ValueObject.html

[Evans] *"Many objects have no conceptual identity. These objects describe characteristics of a thing."*

#### Events
[TODO]

#### Aggregate
An aggregate is a cluster of domain objects that can be treated as a single unit.
An example is an order and its lineitems, these will be separate objects, but it's useful to treat the order (together with its lineitems) as a single aggregate.
Any references from outside the aggregate should only go to the aggregate root. The root can thus ensure the integrity of the aggregate as a whole.
All Entities should be marked with the IAggregateRoot interface

#### Repository
No clear generic repository and interface are defined, each service and it's model are free to define the
shape (CRUD) of the repositories. Important is that they only return or accept Domain Entities.
DbContext should not be exposed, can only be used internaly.
(https://martinfowler.com/bliki/DDD_Aggregate.html)

#### Services [TODO]

#### BusinessRule
Used to encapsulate certain rules in the Domain, which makes it clearer to reason about. A rule needs to implement IBusinessRule::IsSatisfied().
Each rule can be independently tested. The rules should have meaningfull names.
Rules help making Entity methods itself less complex.

- Check.Throw(rule): throws when rule not satisfied
- Check.Return(rule): returns false when rule not satisfied
```
    (Entity/ValueObject)
    public void SetName(string name)
    {
        EnsureArg.IsNotNullOrEmpty(name, nameof(name));
        Check.Throw(new NameShouldBePrefixedWithZipCodeRule(name));

        this.Name = name;
    }
```

```
    (BusinessRule)
    public class NameShouldBePrefixedWithZipCodeRule : IBusinessRule
    {
        private readonly string name;

        public NameShouldBePrefixedWithZipCodeRule(string name)
        {
            this.name = name;
        }

        public string Message => "Name should be prefixed with zipcode";

        public bool IsSatisfied()
        {
            return Regex.IsMatch(this.name, @"^\d+");
        }
    }
```

# Infrastructure
```

                                             http:5002  http:5006
               +==============+-------------------|-------------|--------------.
               | DOCKER HOST  |                   |             |              |
               +==============+                   V             |              |
 .----.        |                                .------------.  |              |
 |    |        |                     .--------->| Customers  |  |              |
 | C -|        |                     |  http:80 |  Service   |  |              |
 | L -|  https |        .----------. |          `------------`  |              |
 | I -|   6100 |    433 | Api      | |                          |              |
 | E -|---------------->| Gateway  |-`                          |              |
 | N -|   http |     80 |==========|                            V              |
 | T -|   6000 |        | (ocelot) |-.                .------------.           |
 | S -|        |        `----------`  `-------------->| Orders     |           |
 |    |        |                              http:80 |  Service   |           |
 `----`        |                                      `------------`           |
               |                                                               |
               `---------------------------------------------------------------`

```

### Authentication
Token based authentication benefits API based systems by enhancing overall security,
eliminating the use of system (privileged) accounts, providing a secure audit mechanism and supporting advanced authentication use cases.

- The access token is acquired by requesting it from the identity provider (keycloak /token endpoint)
- The access token is a digitally signed bearer token (JWT)
- All systems are part of the same security realm (use the same identity provider)
- Every system actor (ApiGateway, Services) must validate the identity token (authenticate the request).
- This token validation includes audience restriction enforcement, which further ensures the token is used where it is supposed to be

##### OpenID Connect specification
OAuth2 provides delegated authorization. OpenID Connect adds federated identity on top of OAuth2. Together, they offer a standard spec to code against and have confidence that it will work across IdPs (Identity Providers).


```
.----------------.
| Client         |  (1)                                                        =Identity Provider
|=============== |-----------------------------------.                  .------------------------.
| (frontend or   |-----.                              \                 | OAuth2 Server          |
|  other service |  (2) \                              \                | & OIDC Provider        |
`----------------`       \     .-----------------.      \               |                        |
                          `--->| Service         |       \              |------------------------|
                               |          (5)(7) |        `------------>| token endpoint         |
                               |=================|                      |------------------------|
                               | (relying party) |--.                   | authorization endpoint |
                               |                 |-. \ (3)              |------------------------|
                               |                 |. \ \                 | OIDC configuration     |
                               `-----------------` \ \ `--------------->| endpoint               |
                                                    \ \ (4)             |------------------------|
                                                     \ `--------------->| JWKS endpoint          |
                                                      \ (6)             |------------------------|
                                                       `--------------->| userinfo endpoint      |
(1) Obtain id_token & access_token from Identity Provider               `------------------------`
(2) Call Service, provide obtained access_token (JWT) in authorization header
(3) Discover OIDC Provider metadata/configuration (/.well-known/openid-configuration)
(4) Get JSON Web Key Set (JWKS) for signature keys
(5) Validate access_token (JWT)
(6) Get additional user attributes with access_token from userinfo endpoint
(7) Service can access Identity and it's claims, roles and userinfo

```

Example requests to obtain the tokens:

```
POST {{baseUrl}}/token HTTP/1.1
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials
&client_id=[CLIENTID]
&client_secret=[CLIENTSECRET]
```

```
POST {{baseUrl}}/token HTTP/1.1
Content-Type: application/x-www-form-urlencoded

grant_type=password
&client_id=[CLIENTID]
&client_secret=[CLIENTSECRET]
&username=[USERNAME]
&password=[PASSWORD]
```

##### Tokens
```
                                         JWT    .-----------.
              JWT                    (4) bearer | Customers |
   .----. (2) bearer  .----------.  .---------->|  Service  |
   |    |------------>| Api      |_/     token  `-----------`
   | C -|     token   | Gateway  | \
   | L -|             `----------`  \          .------------.
   | I -|              (3) forward   `-------->|  Orders    |
   | E -|                                      |   Service  |
   | N -|                .-----------.         `------------`
   | T -| (1) obtain     | Identity  |
   | S -|--------------->| Provider  |
   |    |   access token |========== |
   `----`                | (keycloak)|
                         `-----------`
```

### Deployment (CI/CD)

- Azure Pipelines: https://vip32.visualstudio.com/Zeta
- Solution build script: [azure-pipelines.yml](azure-pipelines.yml)

#### Overview
```

                           .----------------------------------------.
                           | .------------------------------------. |
                           | | .--------------------------------. | |
                           | | | .----------------------------. | | |
         |                 | | | | .------------------------. | | | |
         |                 | | | | |  .------------------.  | | | | |
         |                 | | | | |  |      Code        |  | | | | |
         |                 | | | | |  `------------------`  | | | | |
         |                 | | | | |        Service         | | | | |
         V                 | | | | `------------------------` | | | |
       Build  -->          | | | |     +++++++++++++++++      | | | |
       Deploy <--          | | | |     +++ Container +++      | | | |  
         |                 | | | `----------------------------` | | |
         |                 | | |            Cluster             | | |
         |                 | | `--------------------------------` | |
         |                 | |                VM                  | | 
         |                 | `------------------------------------` | 
         V                 |          Cloud/Datacenter/Local        |
                           `----------------------------------------`

```

#### Pipelines
```

               .--------------.                                          .--------------------.
               | Azure Devops |                                          | Linux VM           |
               | Pipeline     |           .------------.                 | +docker-compose    |
               |              |           | Azure      |                 | -or- Azure WebApps | -or- Kubernetes Cluster
               | [build]      |           | Container  |                 `--------------------`
               `--------------`           | Registry   |<<===============- pull
                   - publish -==========>>|            |
                                          | [images]   |
                                          `------------`

```

## Services

#### ApiGateway
- health: https://localhost:6100/health
    - https://customers.presentation.web/health (port 80)
    - https://orders.presentation.web/health (port 80)

#### Customers
- api gateway: https://localhost:6100/customers/api/values -> https://customers.presentation.web/api/values (port 80)
- local:  http://localhost:5002/api/values (debugging only)

#### Orders
- api gateway: https://localhost:6100/customers/api/values -> https://orders.presentation.web/api/values (port 80)
- local:  http://localhost:5006/api/values (debugging only)

----------------------------------------------------------------------------
----------------------------------------------------------------------------
----------------------------------------------------------------------------

## Docker local

- docker build -t zeta/zeta.sample.services.customers .
- docker image ls
- docker run zeta/zeta.sample.services.customers

- docker network create zeta-network
- docker-compose -f .\docker.compose.yml -f .\docker.compose.override.yml build
- docker-compose -f .\docker.compose.yml -f .\docker.compose.override.yml up -d

## Docker interactive
- `docker run --rm -it -v %cd%:/Zeta mcr.microsoft.com/dotnet/core/sdk dotnet`

## Docker VM
- https://blog.docker.com/2019/08/deploy-dockerized-apps-without-being-a-devops-guru/
- https://docs.microsoft.com/en-us/azure/virtual-machines/linux/docker-compose-quickstart
- https://buildazure.com/how-to-setup-an-ubuntu-linux-vm-in-azure-with-remote-desktop-rdp-access/
- https://azure.github.io/AppService/2018/06/27/How-to-use-Azure-Container-Registry-for-a-Multi-container-Web-App.html
- https://docs.microsoft.com/en-us/aspnet/core/security/docker-https?view=aspnetcore-2.2
-
##### create linux docker vm (azure console)
- `az account set --subscription [SUBSCRIPTIONID]`
- `az group create --name globaldocker --location westeurope`
- `az vm create --resource-group globaldocker --name globaldockervm --image UbuntuLTS --admin-username [USERNAME] --generate-ssh-keys --custom-data cloud-init.txt`
- `az vm open-port --port 80 --priority 900 --nsg-name globaldockervmNSG --resource-group globaldocker --name globaldockervm`
- `az vm open-port --port 443 --priority 901 --nsg-name globaldockervmNSG --resource-group globaldocker --name globaldockervm`
- `az vm open-port --port 6000 --priority 1100 --nsg-name globaldockervmNSG --resource-group globaldocker --name globaldockervm`
- `az vm open-port --port 6100 --priority 1101 --nsg-name globaldockervmNSG --resource-group globaldocker --name globaldockervm`
- `az vm open-port --port 9000 --priority 1102 --nsg-name globaldockervmNSG --resource-group globaldocker --name globaldockervm`

##### install docker (terminal)
- `sudo apt install gnupg2 pass` # due to issue https://github.com/docker/cli/issues/1136
- `sudo apt install docker-compose`

##### setup ubuntu desktop + rdp (terminal)
- `sudo apt install mc`
- `sudo apt-get install lxde -y`
- `sudo apt-get install xrdp -y`
- `/etc/init.d/xrdp start`
- remote client: rdp into [vmIP]:3389

##### setup docker management application (terminal)
- `docker volume create portainer_data`
- `docker run -d -p 8000:8000 -p 9000:9000 --restart unless-stopped -v /var/run/docker.sock:/var/run/docker.sock -v portainer_data:/data portainer/portainer`
- remote client: browse to [vmIP]:9000 (portainer)

##### install dotnet (terminal)
- install dotnet core 2.2 https://dotnet.microsoft.com/download/linux-package-manager/ubuntu18-04/sdk-current
  - `wget -q https://packages.microsoft.com/config/ubuntu/18.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb`
  - `sudo dpkg -i packages-microsoft-prod.deb`
  - `sudo add-apt-repository universe`
  - `sudo apt-get update`
  - `sudo apt-get install apt-transport-https`
  - `sudo apt-get update`
  - `sudo apt-get install dotnet-sdk-`2.2`
  - `sudo apt-get install dotnet-sdk-3.0`
- export the host dev cert https://docs.microsoft.com/en-us/aspnet/core/security/docker-https?view=aspnetcore-2.2
  - `dotnet dev-certs https -ep ${HOME}/.aspnet/https/aspnetapp.pfx -p [PFX_PASSWORD]`

##### setup docker compose (terminal)
- `nano docker-compose.yml`

```
version: '3.4'

services:
  apigateway.presentation.web:
    image: globaldockerregistry.azurecr.io/zeta/apigateway.presentation.web
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=https://+;http://+
      - ASPNETCORE_HTTPS_PORT=443
      - ASPNETCORE_Kestrel__Certificates__Default__Password=[PFX_PASSWORD]
      - ASPNETCORE_Kestrel__Certificates__Default__Path=/https/aspnetapp.pfx
    ports:
      - 80:80
      - 443:443
    volumes:
      - ${HOME}/.aspnet/https:/https/ # dev cert

  customers.presentation.web:
    image: globaldockerregistry.azurecr.io/zeta/customers.presentation.web
    ports:
      - 6001:80

  orders.presentation.web:
    image: globaldockerregistry.azurecr.io/zeta/orders.presentation.web
    ports:
      - 6002:80

#web:
#  image: nginxdemos/hello
#  ports:
#    - 80:80
```

- `sudo docker login -u [USERNAME] -p [PASSWORD] globaldockerregistry.azurecr.io` # login to registry
- `sudo docker pull globaldockerregistry.azurecr.io/zeta/orders.presentation.web` # test
- `sudo docker rmi globaldockerregistry.azurecr.io/zeta/orders.presentation.web` # test
- `sudo docker-compose up -d` # start the docker-compose.yml

- remote client: browse to http://[vmIP] (apigateway) # verify
- remote client: browse to https://[vmIP] (apigateway) # verify




TODO:
- add a reverse proxy ([howto](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx?view=aspnetcore-3.0))

Identity Provider
- keycloak docker compose + sql : https://github.com/keycloak/keycloak-containers/blob/master/docker-compose-examples/keycloak-mssql.yml
- aspnetcore+keycloak (gateway) : https://stackoverflow.com/questions/41721032/keycloak-client-for-asp-net-core/43875291#43875291
                                  https://github.com/Gimly/SampleNetCoreAngularKeycloak