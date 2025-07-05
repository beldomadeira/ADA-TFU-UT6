# No shebang since this does not use any shell features.
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
