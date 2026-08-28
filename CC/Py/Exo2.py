n = 0
maximum = float('-inf')
minimum = float('inf')

nombres = []

print("Entrer 5 nombres :")

while n < 5:
    i = int(input())
    n += 1
    nombres.append(i)

for nombre in nombres:
    if nombre > maximum:
        maximum = nombre

for nombre in nombres:
    if nombre < minimum:
        minimum = nombre

print(f"Le plus grand nombre est {maximum}")
print(f"Le plus petit nombre est {minimum}")