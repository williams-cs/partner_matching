open System
open CsvHelper
open CSVUtil
open System.IO
open System.Collections.Generic
open System.Text
open System.Security.Cryptography

let adict(a: seq<('a*'b)>) = new Dictionary<'a,'b>(a |> dict)

let swap(a: _[])(x: int)(y: int) : unit =
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

// pairs a student ID with a random ordering of other student names
let randomPrefs(student: Student)(students: Student[])(r: Random) : string*string[] =
    let arr = [| 0 .. students.Length - 1 |]
    shuffle arr r
    student.ID, arr |> Array.map (fun idx -> students.[idx].Name)

// for each student ID, returns a rank ordering of preferred partners,
// from most to least wanted
// key: student ID
// value: student name
let randomPreferences(students: Student[])(r: Random) : Dictionary<string,string[]> =
    students
    |> Array.map (fun s -> randomPrefs s students r)
    |> adict

let findAndSwap(nos: string[])(myPrefs: string[]) : string[] =
    let mp = Array.copy myPrefs
    for i in 0 .. nos.Length - 1 do
        // get anti-student
        let no = nos.[i]
        // get index of student in mp
        let noidx = Array.IndexOf(mp,no)
        // swap with the next last element
        swap mp noidx (mp.Length - i - 1)
    mp

let readAntiPreferences(path: String option) : AntiPreference[] =
    match path with
    | Some p ->
        use reader = new StreamReader(p)
        use csv = new CsvReader(reader)
        let f = Func<string,int,string>(fun header _ ->
                                                header
                                                    .ToLower()
                                                    .Replace(" ", "")
                                                    .Replace("(", "")
                                                    .Replace(")", "")
                                                    .Replace("#", "")
                                       )
        csv.Configuration.PrepareHeaderForMatch <- f
        csv.GetRecords<AntiPreference>() |> Seq.toArray
    | None -> [||]

// returns a dictionary where each student ID represents the key
// and the value represents a rank ordering of preferred student
// IDs for that student
let assignPreferences(antiprefs: AntiPreference[])(students: Student[])(r: Random) : Dictionary<string,string[]> =
    // initially assign randomly
    let rPrefs = randomPreferences students r

    // get student-by-name dictionary
    let sByName = Student.StudentsByName students

    // get student-by-email directory
    let sByEmail = Student.StudentsByEmail students

    // get student-by-ID directory
    let sByID = Student.StudentsByID students

    // track reflexive relation
    let reflexiveAntiprefs = new Dictionary<string,HashSet<string>>()

    // for each antipref list
    // swap antipreferences for student with next lowest random preference
    let aps =
        antiprefs |>
            Array.map (fun ap ->
                // lookup student by email address and get student ID
                // student may not be in section, in which case, skip
                if sByEmail.ContainsKey(ap.EmailAddress) then
                    let sID = sByEmail.[ap.EmailAddress].ID
                    // get list of students this student does not want to work with
                    let nos = ap.AsArray

                    // filter students who are not in this section
                    let nos' = nos |> Array.filter (fun sname -> sByName.ContainsKey sname)

                    // add nos' to reflexive antipref dict
                    for name in nos' do
                        let apSID = sByName.[name].ID
                        if not (reflexiveAntiprefs.ContainsKey apSID) then
                            reflexiveAntiprefs.Add(apSID, new HashSet<string>())
                        reflexiveAntiprefs.[apSID].Add(sByID.[sID].Name) |> ignore

                    // get the students assigned preferences
                    Some(sID, findAndSwap nos' rPrefs.[sID])
                else
                    None
            ) |>
            Array.choose id
    // turn into dict
    let apd = aps |> adict

    // make relation reflexive
    let rapd =
        reflexiveAntiprefs |>
            Seq.map (fun kvp ->
                let sID = kvp.Key
                let nos = kvp.Value |> Seq.toArray
                sID, findAndSwap nos rPrefs.[sID]
            ) |> adict

    // replace prefs with antiprefs where appropriate
    let rPrefs' =
        rPrefs |>
            Seq.map (fun kvp ->
                if apd.ContainsKey(kvp.Key) then
                    // antipreference
                    kvp.Key, apd.[kvp.Key]
                else if rapd.ContainsKey(kvp.Key) then
                    // reflexive antipreference
                    kvp.Key, rapd.[kvp.Key]
                else
                    // no preference
                    kvp.Key, kvp.Value
            ) |> adict
    rPrefs'

let studentsByIndex(students: Student[]) : Dictionary<int,Student> =
    students |> Array.mapi (fun i student -> i, student) |> adict

let numAntiPrefs(students: Student[])(antiprefs: AntiPreference[]) : Dictionary<Student,int> =
    let pByEmail = AntiPreference.antiPrefsByEmail antiprefs
    let d = new Dictionary<Student,int>()
    for student in students do
        let email = student.Email
        if pByEmail.ContainsKey email then
            let ap = pByEmail.[email]
            let cnt = ap.AntiPrefCount
            d.Add(student, cnt)
        else
            d.Add(student,0)
    d

let group_students(students: Student[])(prefs: Dictionary<string,string[]>)(antiPrefs: AntiPreference[]) : HashSet<Student>[] =
    let takenNames = new HashSet<string>();
    let mutable groups = []
    let sByName = Student.StudentsByName students
    let apCount = numAntiPrefs students antiPrefs

    let students' = 
        students |>
        Array.sortWith
            (fun s1 s2 ->
                if apCount.[s1] > apCount.[s2] then
                    -1
                else if apCount.[s1] = apCount.[s2] then
                    0
                else
                    1
            )

    for student in students' do
        // get ID
        let sID = student.ID

        // get name
        let sName = student.Name

        // if student was already paired, skip
        if not (takenNames.Contains sName) then
            // mark student as taken
            takenNames.Add sName |> ignore

            // get preferences
            let sPrefs = prefs.[sID]

            // while their top preference is taken and preference is not
            // themselves, pick next pref
            let mutable nextTop = 0
            while nextTop < sPrefs.Length &&                // we haven't run out of partners
                  (takenNames.Contains(sPrefs.[nextTop]) ||  // the partner is not already chosen
                   sName = sPrefs.[nextTop]) do             // the partner is not the student themselves
                nextTop <- nextTop + 1

            // sometimes there is no next preference; 
            // this is the odd student who cannot find a partner
            if nextTop = sPrefs.Length then
                // add to the last group added
                let g = List.head groups
                groups <- (student :: g) :: List.tail groups
                //let s = String.Join(" and ", g |> List.map (fun s -> s.Name))
                //printfn "Partnering %s with %s" (student.Name) s
            else 
                //printfn "Partnering %s with %s" (student.Name) (sPrefs.[nextTop])

                takenNames.Add sPrefs.[nextTop] |> ignore
                groups <- [student; sByName.[sPrefs.[nextTop]]] :: groups

    groups |>
        List.map (fun group ->
            let hsg = new HashSet<Student>()
            group |> List.iter (fun student -> hsg.Add student |> ignore)
            hsg
        ) |>
        List.toArray

let usage() =
    printfn "Usage: dotnet run <roster csv> <random seed> <anti-preference csv>"
    //printfn "\twhere [flags]:"
    //printfn "\t--verbose\tAlso include real names in CSV output."
    exit 1

[<EntryPoint>]
let main argv =
    // parse args
    if argv.Length < 2 || argv.Length > 4 then
        usage()
    let rosterpath = argv.[0]
    let prefpath =
        match argv.Length with
        | 2 -> None
        | 3 -> Some argv.[2]
        | _ -> usage()
    let seed = sha1 argv.[1]

    // init RNG using seed
    let r = new Random(seed)

    // read roster CSV
    let students = readRoster rosterpath

    // read antiprefs CSV
    let antiprefs = readAntiPreferences prefpath

    // read preferences, taking into account antipreferences
    let prefs = assignPreferences antiprefs students r

    // shuffle students in-place (so that no student always gets their first choice)
    shuffle students r

    // pair
    let groups = group_students students prefs antiprefs

    for group in groups do
        let githubs = group |> Seq.map (fun s -> s.Github)
        let s = String.Join(",", githubs)
        printfn "%s" s
    
    0
