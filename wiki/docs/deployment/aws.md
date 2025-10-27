---
sidebar_position: 3
---

# Deploy to AWS (ECS Fargate)

Minimal instructions to deploy a Swap-generated Docker image to AWS ECS Fargate.

> A full guide with screenshots and IaC samples is coming soon. This page is a placeholder to satisfy documentation links.

## Prerequisites
- AWS account and CLI configured
- Docker installed

## Quick Start

1) Build your image
```bash
docker build -t myapp:latest .
```

2) Push to ECR
```bash
AWS_ACCOUNT_ID=123456789012
AWS_REGION=us-east-1
REPO=myapp

aws ecr create-repository --repository-name $REPO --region $AWS_REGION || true
aws ecr get-login-password --region $AWS_REGION | docker login --username AWS --password-stdin $AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com

docker tag myapp:latest $AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/$REPO:latest
docker push $AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/$REPO:latest
```

3) Create an ECS Fargate service (high-level outline)
- Create a VPC + subnets (or reuse existing)
- Create an ECS cluster
- Create a task definition referencing your ECR image
- Create a Fargate service (desired count = 1) with a public ALB

4) Configure environment
- Set connection strings as task env vars or AWS Secrets Manager
- Ensure your database is reachable from the service (Security Groups/VPC)

5) Roll out updates
- Build, tag, push new image
- Update the ECS service to use the new image
