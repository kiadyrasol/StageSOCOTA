def analyser_commandes(commandes):
    total_revenue = 0
    best_product = ""
    best_revenue_product = 0

    spending_by_client = {}

    product_sold = 0

    for commande in commandes:
        price = commande["prix"]
        quantity = commande["quantite"]
        product = commande["produit"]
        name = commande["client"]

        # 1. Total revenue
        revenue_per_order = price * quantity
        total_revenue += revenue_per_order

        # 2. Product with the highest revenue
        if revenue_per_order > best_revenue_product:
            best_revenue_product = revenue_per_order
            best_product = product

        # 3. Spending by customer
        if name not in spending_by_client:
            spending_by_client[name] = revenue_per_order
        else:
            spending_by_client[name] += revenue_per_order

        # 4. Total products sold
        product_sold += quantity

    # Find the customer who spent the most
    client_name = ""
    client_buy = 0

    for name, amount in spending_by_client.items():
        if amount > client_buy:
            client_buy = amount
            client_name = name

    return total_revenue, best_product, client_name, product_sold