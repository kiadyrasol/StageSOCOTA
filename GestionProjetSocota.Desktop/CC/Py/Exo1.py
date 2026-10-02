def calculer_ventes(ventes):
      #chiffre d'affaire = qtt * prix
      chiffre_affaire = 0
      produit_rentable = ""
      expensive = 0
  
      for vente in ventes:
            quantite = vente["quantite"]
            prix = vente["prix"]
            nom = vente["produit"]

            ca_produit = quantite * prix
            chiffre_affaire += ca_produit

            if ca_produit > expensive :
                  expensive = ca_produit
                  produit_rentable = nom
                  capr = prix * quantite
 
      return chiffre_affaire, produit_rentable, capr
  
ventes = [
    {"produit": "Écran", "quantite": 2, "prix": 300},
    {"produit": "PC", "quantite": 3, "prix": 1200},
    {"produit": "Téléphone", "quantite": 5, "prix": 800},
    {"produit": "Clavier", "quantite": 10, "prix": 50}
]

ca, pr, capr = calculer_ventes(ventes)
print(f"CA : {ca}")
print(f"Produit le plus rentable : {pr}")
print(f"CA du produit le plus rentable : {capr}")