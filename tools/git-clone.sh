#!/bin/bash
REPOSITORIES=(Zeta.ApiGateway Zeta.Services.Availability Zeta.Services.Customers Zeta.Services.Deliveries Zeta.Services.Identity Zeta.Services.Operations Zeta.Services.Orders Zeta.Services.OrderMaker Zeta.Services.Parcels Zeta.Services.Pricing Zeta.Services.Vehicles)

for REPOSITORY in ${REPOSITORIES[*]}
do
	 echo ========================================================
	 echo Cloning the repository: $REPOSITORY
	 echo ========================================================
	 REPO_URL=https://github.com/vip32/$REPOSITORY.git
	 git clone $REPO_URL .\services
	 cd $REPOSITORY && cd ..
done