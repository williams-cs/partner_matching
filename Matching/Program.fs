open System
open CsvHelper
open CSVUtil
open System.IO
open System.Collections.Generic
open System.Text
open System.Security.Cryptography

let adict(a: seq<('a*'b)>) = new Dictionary<'a,'b>(a |> dict)

let swap(a: _[]) x y =
    let tmp = a.[x]
    a.[x] <- a.[y]
    a.[y] <- tmp

let shuffle(a: 'a[])(rand: Random) =
    Array.iteri (fun i _ -> swap a i (rand.Next(i, Array.length a))) a

let sha1(txt: string) : int =
    use sha1 = new SHA1Managed()
    let hash : byte[] = sha1.ComputeHash (Encoding.UTF8.GetBytes txt)
    BitConverter.ToInt32(hash, 0)

let readRoster(path: string) : Student[] =
    use reader = new StreamReader(path)
    use csv = new CsvReader(reader)
    let f = Func<string,int,string>(fun header index -> header.ToLower().Replace(" ", ""))
    csv.Configuration.PrepareHeaderForMatch <- f
    csv.GetRecords<Student>() |> Seq.toArray |> Array.sortBy (fun s -> (s.LastName, s.FirstName))

let randomPreferences(students: Student[])(r: Random) : Dictionary<string,int[]> =
    students
    |> Array.map (fun student ->
                    let arr = [| 0 .. students.Length - 1 |]
                    shuffle arr r
                    student.ID, arr
                 )
    |> adict

let studentsByIndex(students: Student[]) : Dictionary<int,Student> =
    students |> Array.mapi (fun i student -> i, student) |> adict

let pair(students: Student[])(prefs: Dictionary<string,int[]>)(sByIndex: Dictionary<int,Student>) : Dictionary<Student,Student> =
    // track student availability (mutable)
    let available = new HashSet<Student>(students)

    // track pairings (mutable)
    let pairing = new Dictionary<Student,Student>()

    for student in students do
        let mutable complete = false
        let mutable i = 0
        let sprefs = prefs.[student.ID]
        while not complete && i < students.Length do
            let pref = sprefs.[i]
            let partner = sByIndex.[pref]
            if student <> partner && available.Contains partner then
                available.Remove student |> ignore
                available.Remove partner |> ignore
                complete <- true
                pairing.Add(student, partner)
            else
                i <- i + 1
                
    pairing

[<EntryPoint>]
let main argv =
    if argv.Length <> 2 then
        printfn "Usage: dotnet run <csv> <random seed>"
        exit 1
    let path = argv.[0]
    let seed = sha1 argv.[1]

    // init RNG using seed
    let r = new Random(seed)

    // read CSV
    let students = readRoster path

    // assign preferences randomly
    let prefs = randomPreferences students r
    let sByIndex = studentsByIndex students

    // shuffle students in-place (so that no student always gets their first choice)
    shuffle students r

    // pair
    let pairing = pair students prefs sByIndex

    // sort pairings by first student last name, first name
    let pairing_sorted = pairing |> Seq.sortBy (fun pair -> (pair.Key.LastName, pair.Key.FirstName))

    for pair in pairing_sorted do
        printfn "%s %s, %s %s" pair.Key.FirstName pair.Key.LastName pair.Value.FirstName pair.Value.LastName
    
    0
