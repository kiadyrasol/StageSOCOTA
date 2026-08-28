compteurpair = 0
compteurimpair = 0
n = 0
nombres = []
print("Entrer 5 nombres \n")

while n < 5 :
    i = int(input())
    n +=1
    nombres.append(i)
    
for nombre in nombres:
    if (nombre % 2 == 0):
        compteurpair += 1
    else:
        compteurimpair +=1

print(f"Il y a {compteurpair} nombre(s) pair(s) et {compteurimpair} nombre(s) impair(s).")

