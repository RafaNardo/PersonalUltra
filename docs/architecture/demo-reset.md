# Reset da demonstração (M1)

`POST /api/v1/demo/reset` existe apenas para recuperar o estado determinístico
da demonstração durante desenvolvimento. Ele requer autenticação de desenvolvimento,
ambiente `Development` e `DemoData:AllowReset=true`.

Antes de apagar e reseedar a base, o servidor verifica que a base contém somente
o usuário e membro de demonstração conhecidos. Se existir qualquer outro usuário
ou membro, o endpoint retorna `DEMO_RESET_NOT_SAFE` e não altera dados.

O endpoint não é mapeado de forma utilizável em ambientes não-Development nem
quando a flag explícita está desabilitada. Não deve ser habilitado para produção
ou para uma base compartilhada.
