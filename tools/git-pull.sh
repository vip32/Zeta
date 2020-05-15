#!/bin/bash
REPOSITORIES=(Zeta.ApiGateway Zeta.Services.Availability Zeta.Services.Customers Zeta.Services.Deliveries Zeta.Services.Identity Zeta.Services.Operations Zeta.Services.Orders Zeta.Services.OrderMaker Zeta.Services.Parcels Zeta.Services.Pricing Zeta.Services.Vehicles)

for REPOSITORY in ${REPOSITORIES[*]}
do
	 echo ========================================================
	 echo Updating the repository: $REPOSITORY
	 echo ========================================================
	 cd $REPOSITORY && git checkout develop && git pull && git checkout master && git pull && cd ..
done