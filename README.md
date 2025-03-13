# Dataset
https://www.cs.cmu.edu/~enron/enron_mail_20150507.tar.gz
https://www.cs.cmu.edu/~enron/


# Docker

To initialize a Docker swarm, you can use the following command:
```bash
docker swarm init
```

To deploy the application fully in Docker (with multiple replicas of services), you can use the following commands:
```bash
docker stack deploy -c docker-swarm.yaml omniscient-node     
```

To see the status of the services, you can use the following command:
```bash
docker service ls
```

To see containers in the stack, you can use the following command:
```bash
docker stack ps omniscient-node
```

To remove the stack, you can use the following command:
```bash
docker stack rm omniscient-node
```