# بسم الله الرحمن الرحیم - Besmellah, Alrahman, Alrahim

## CSharp Alla

### A simple functional programming language like lua written in c#

You can write all your codes in a single line.

#### Variables

``` lua
name = "Mahdi" family = "Khzalilzadeh"
fullname = name + " " + family
writeline(fullname) # output: Mahdi Khalilzadeh
```

#### Operations and comparisons

``` lua
num1 = 10 num2 = 20
sum = num1 * 5 / (num2 + num1 - (num1 + 1)) + 10 + num2 / num2

fullname = "Mahdi Khalilzadeh"
isEqual = fullname != "mahdi khalilzadeh"
    && num1 != num2 || false || (10 - 2) + 1 == 50 # true
```

#### Functions

``` lua
function sum(a,b) {
    return a + b
}

function run(t) {
    return sum(t, t * 2)
}

write("Result: ")
writeline(run(10))
```

If I had life and time :) I would develop more...

sorry for bad eng, im still learning.
